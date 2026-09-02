using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using DotNetEnv;
using FinanceDashboardApi.Models;
using FinanceDashboardApi.Services;
using Google.Apis.Auth;
using Npgsql;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// ENVIRONMENT
// =====================================================

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var authSecret = Environment.GetEnvironmentVariable("AUTH_SECRET") ?? "finance-dashboard-secret-change-this";
var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");

var npgsqlConnectionString = databaseUrl is null ? null : ToNpgsqlConnectionString(databaseUrl);

// =====================================================
// SERVICES
// =====================================================

if (npgsqlConnectionString is not null)
{
    builder.Services.AddNpgsqlDataSource(npgsqlConnectionString);
}

builder.Services.AddSingleton(new TokenService(authSecret));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors();

// An unhandled exception bypasses the normal response pipeline, which
// means the CORS middleware never gets a chance to attach its headers —
// the browser then reports a confusing "CORS policy" error that hides
// the real 500. Catching everything here keeps every response, including
// failures, flowing through the normal (CORS-tagged) path.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception error)
    {
        Console.WriteLine($"Unhandled exception on {context.Request.Method} {context.Request.Path}: {error}");

        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { detail = "Internal server error." });
        }
    }
});

// =====================================================
// DATABASE INITIALIZATION
// =====================================================

if (npgsqlConnectionString is not null)
{
    try
    {
        await InitializeDatabaseAsync(npgsqlConnectionString);
        Console.WriteLine("Database initialized successfully.");
    }
    catch (Exception error)
    {
        Console.WriteLine($"Database initialization failed: {error.Message}");
    }
}
else
{
    Console.WriteLine("WARNING: DATABASE_URL is not configured.");
}

Console.WriteLine("Finance Dashboard API started.");

// =====================================================
// HOME
// =====================================================

app.MapGet("/", () => Results.Ok(new { message = "Finance Dashboard API is running" }));

// =====================================================
// HEALTH
// =====================================================

app.MapGet("/api/health", async (NpgsqlDataSource? db) =>
{
    if (databaseUrl is null)
    {
        return Results.Ok(new { status = "ok", database = "not configured" });
    }

    try
    {
        await using var connection = await db!.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("SELECT 1", connection);
        await command.ExecuteScalarAsync();

        return Results.Ok(new { status = "ok", database = "connected" });
    }
    catch (NpgsqlException)
    {
        return Results.Problem(statusCode: 503, detail: "Database connection failed");
    }
});

// =====================================================
// REGISTER
// =====================================================

app.MapPost("/api/register", async (RegisterRequest request, NpgsqlDataSource? db) =>
{
    var validation = Validate(request);
    if (validation is not null) return validation;

    var username = request.Username.Trim();
    var email = request.Email.Trim().ToLowerInvariant();

    if (string.IsNullOrEmpty(username))
    {
        return Results.Problem(statusCode: 400, detail: "Username is required.");
    }

    try
    {
        await using var connection = await db!.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        await using (var checkUsername = new NpgsqlCommand(
            "SELECT id FROM users WHERE LOWER(username) = LOWER(@username) LIMIT 1", connection, transaction))
        {
            checkUsername.Parameters.AddWithValue("username", username);
            if (await checkUsername.ExecuteScalarAsync() is not null)
            {
                return Results.Problem(statusCode: 409, detail: "Username already exists.");
            }
        }

        await using (var checkEmail = new NpgsqlCommand(
            "SELECT id FROM users WHERE LOWER(email) = LOWER(@email) LIMIT 1", connection, transaction))
        {
            checkEmail.Parameters.AddWithValue("email", email);
            if (await checkEmail.ExecuteScalarAsync() is not null)
            {
                return Results.Problem(statusCode: 409, detail: "Email already exists.");
            }
        }

        await using var insert = new NpgsqlCommand(
            """
            INSERT INTO users (username, email, password_hash)
            VALUES (@username, @email, @passwordHash)
            RETURNING id, username, email
            """, connection, transaction);

        insert.Parameters.AddWithValue("username", username);
        insert.Parameters.AddWithValue("email", email);
        insert.Parameters.AddWithValue("passwordHash", PasswordHasher.Hash(request.Password));

        await using var reader = await insert.ExecuteReaderAsync();
        await reader.ReadAsync();

        var result = new
        {
            success = true,
            message = "Account created successfully.",
            id = reader.GetInt64(0),
            username = reader.GetString(1),
            email = reader.GetString(2)
        };

        await reader.CloseAsync();
        await transaction.CommitAsync();

        return Results.Ok(result);
    }
    catch (NpgsqlException)
    {
        return Results.Problem(statusCode: 503, detail: "Could not create account.");
    }
});

// =====================================================
// LOGIN (both /api/login and /api/auth/login, matching main.py's
// double @app.post decorator on the same handler)
// =====================================================

var loginHandler = async (LoginRequest request, NpgsqlDataSource? db, TokenService tokens) =>
{
    var validation = Validate(request);
    if (validation is not null) return validation;

    var username = request.Username?.Trim() ?? "";
    var email = request.Email?.Trim().ToLowerInvariant() ?? "";

    if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(email))
    {
        return Results.Problem(statusCode: 400, detail: "Username or email is required.");
    }

    try
    {
        await using var connection = await db!.OpenConnectionAsync();

        await using var command = new NpgsqlCommand(
            string.IsNullOrEmpty(email)
                ? "SELECT id, username, email, password_hash FROM users WHERE LOWER(username) = LOWER(@value) LIMIT 1"
                : "SELECT id, username, email, password_hash FROM users WHERE LOWER(email) = LOWER(@value) LIMIT 1",
            connection);

        command.Parameters.AddWithValue("value", string.IsNullOrEmpty(email) ? username : email);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return Results.Problem(statusCode: 401, detail: "Invalid username/email or password.");
        }

        var userId = reader.GetInt64(0);
        var storedUsername = reader.GetString(1);
        var storedEmail = reader.GetString(2);
        var storedHash = reader.IsDBNull(3) ? null : reader.GetString(3);

        if (!PasswordHasher.Verify(request.Password, storedHash))
        {
            return Results.Problem(statusCode: 401, detail: "Invalid username/email or password.");
        }

        return Results.Ok(new
        {
            success = true,
            message = "Login successful.",
            id = userId,
            username = storedUsername,
            email = storedEmail,
            token = tokens.CreateToken(userId)
        });
    }
    catch (NpgsqlException)
    {
        return Results.Problem(statusCode: 503, detail: "Database connection failed.");
    }
};

app.MapPost("/api/login", loginHandler);
app.MapPost("/api/auth/login", loginHandler);

// =====================================================
// GOOGLE LOGIN
// =====================================================

app.MapPost("/api/auth/google", async (GoogleLoginRequest request, NpgsqlDataSource? db, TokenService tokens) =>
{
    if (string.IsNullOrEmpty(googleClientId))
    {
        return Results.Problem(statusCode: 503, detail: "Google sign-in is not configured.");
    }

    GoogleJsonWebSignature.Payload payload;

    try
    {
        payload = await GoogleJsonWebSignature.ValidateAsync(request.Credential, new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = [googleClientId]
        });
    }
    catch (Exception error)
    {
        // The Python original only ever saw ValueError here; the .NET client
        // can also throw for things like a network hiccup fetching Google's
        // public certs, which isn't the token's fault, but a bad/expired
        // credential should still surface as a normal 401, not a crash.
        Console.WriteLine($"Google token verification failed: {error}");
        return Results.Problem(statusCode: 401, detail: "Invalid Google credential.");
    }

    if (!payload.EmailVerified)
    {
        return Results.Problem(statusCode: 401, detail: "Google account email is not verified.");
    }

    var googleSub = payload.Subject;
    var email = payload.Email.Trim().ToLowerInvariant();
    var name = payload.Name ?? email.Split('@')[0];

    try
    {
        await using var connection = await db!.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        long userId;
        string storedUsername;
        string storedEmail;

        await using (var findBySub = new NpgsqlCommand(
            "SELECT id, username, email FROM users WHERE google_sub = @sub LIMIT 1", connection, transaction))
        {
            findBySub.Parameters.AddWithValue("sub", googleSub);
            await using var reader = await findBySub.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                userId = reader.GetInt64(0);
                storedUsername = reader.GetString(1);
                storedEmail = reader.GetString(2);
                await reader.CloseAsync();
            }
            else
            {
                await reader.CloseAsync();

                await using var findByEmail = new NpgsqlCommand(
                    "SELECT id, username, email FROM users WHERE LOWER(email) = LOWER(@email) LIMIT 1", connection, transaction);
                findByEmail.Parameters.AddWithValue("email", email);
                await using var emailReader = await findByEmail.ExecuteReaderAsync();

                if (await emailReader.ReadAsync())
                {
                    userId = emailReader.GetInt64(0);
                    storedUsername = emailReader.GetString(1);
                    storedEmail = emailReader.GetString(2);
                    await emailReader.CloseAsync();

                    await using var link = new NpgsqlCommand(
                        "UPDATE users SET google_sub = @sub WHERE id = @id", connection, transaction);
                    link.Parameters.AddWithValue("sub", googleSub);
                    link.Parameters.AddWithValue("id", userId);
                    await link.ExecuteNonQueryAsync();
                }
                else
                {
                    await emailReader.CloseAsync();

                    var uniqueUsername = await GenerateUniqueUsernameAsync(connection, transaction, name);

                    await using var insert = new NpgsqlCommand(
                        """
                        INSERT INTO users (username, email, password_hash, google_sub)
                        VALUES (@username, @email, NULL, @sub)
                        RETURNING id, username, email
                        """, connection, transaction);
                    insert.Parameters.AddWithValue("username", uniqueUsername);
                    insert.Parameters.AddWithValue("email", email);
                    insert.Parameters.AddWithValue("sub", googleSub);

                    await using var insertReader = await insert.ExecuteReaderAsync();
                    await insertReader.ReadAsync();
                    userId = insertReader.GetInt64(0);
                    storedUsername = insertReader.GetString(1);
                    storedEmail = insertReader.GetString(2);
                    await insertReader.CloseAsync();
                }
            }
        }

        await transaction.CommitAsync();

        return Results.Ok(new
        {
            success = true,
            message = "Login successful.",
            id = userId,
            username = storedUsername,
            email = storedEmail,
            token = tokens.CreateToken(userId)
        });
    }
    catch (NpgsqlException)
    {
        return Results.Problem(statusCode: 503, detail: "Database connection failed.");
    }
});

// =====================================================
// GET CURRENT USER
// =====================================================

app.MapGet("/api/me", async (HttpRequest http, NpgsqlDataSource? db, TokenService tokens) =>
{
    var userId = tokens.GetUserIdFromAuthHeader(http.Headers.Authorization);
    if (userId is null)
    {
        return Results.Problem(statusCode: 401, detail: "Authentication required.");
    }

    try
    {
        await using var connection = await db!.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            "SELECT id, username, email FROM users WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", userId.Value);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return Results.Problem(statusCode: 404, detail: "User not found.");
        }

        return Results.Ok(new
        {
            id = reader.GetInt64(0),
            username = reader.GetString(1),
            email = reader.GetString(2)
        });
    }
    catch (NpgsqlException)
    {
        return Results.Problem(statusCode: 503, detail: "Could not read user.");
    }
});

// =====================================================
// GET TRANSACTION YEARS
// =====================================================

app.MapGet("/api/transactions/years", async (HttpRequest http, NpgsqlDataSource? db, TokenService tokens) =>
{
    var userId = tokens.GetUserIdFromAuthHeader(http.Headers.Authorization);
    if (userId is null)
    {
        return Results.Problem(statusCode: 401, detail: "Authentication required.");
    }

    try
    {
        await using var connection = await db!.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT DISTINCT EXTRACT(YEAR FROM transaction_date)::int AS year
            FROM transactions
            WHERE user_id = @userId
            ORDER BY year DESC
            """, connection);
        command.Parameters.AddWithValue("userId", userId.Value);

        var years = new List<int>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            years.Add(reader.GetInt32(0));
        }

        return Results.Ok(years);
    }
    catch (NpgsqlException)
    {
        return Results.Problem(statusCode: 503, detail: "Could not read transaction years.");
    }
});

// =====================================================
// GET TRANSACTIONS
// =====================================================

app.MapGet("/api/transactions", async (HttpRequest http, int year, NpgsqlDataSource? db, TokenService tokens) =>
{
    if (year is < 2000 or > 2100)
    {
        return Results.Problem(statusCode: 422, detail: "year must be between 2000 and 2100.");
    }

    var userId = tokens.GetUserIdFromAuthHeader(http.Headers.Authorization);
    if (userId is null)
    {
        return Results.Problem(statusCode: 401, detail: "Authentication required.");
    }

    try
    {
        await using var connection = await db!.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT id, type, category, amount, transaction_date
            FROM transactions
            WHERE user_id = @userId
            AND EXTRACT(YEAR FROM transaction_date) = @year
            ORDER BY transaction_date DESC, id DESC
            """, connection);
        command.Parameters.AddWithValue("userId", userId.Value);
        command.Parameters.AddWithValue("year", year);

        var results = new List<object>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new
            {
                id = reader.GetInt64(0),
                type = reader.GetString(1),
                category = reader.GetString(2),
                amount = reader.GetDecimal(3),
                date = reader.GetDateTime(4).ToString("yyyy-MM-dd")
            });
        }

        return Results.Ok(results);
    }
    catch (NpgsqlException)
    {
        return Results.Problem(statusCode: 503, detail: "Could not read transactions.");
    }
});

// =====================================================
// CREATE TRANSACTION
// =====================================================

app.MapPost("/api/transactions", async (HttpRequest http, TransactionCreate transaction, NpgsqlDataSource? db, TokenService tokens) =>
{
    var validation = Validate(transaction);
    if (validation is not null) return validation;

    var userId = tokens.GetUserIdFromAuthHeader(http.Headers.Authorization);
    if (userId is null)
    {
        return Results.Problem(statusCode: 401, detail: "Authentication required.");
    }

    try
    {
        await using var connection = await db!.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO transactions (user_id, type, category, amount, transaction_date)
            VALUES (@userId, @type, @category, @amount, @date)
            RETURNING id, type, category, amount, transaction_date
            """, connection);
        command.Parameters.AddWithValue("userId", userId.Value);
        command.Parameters.AddWithValue("type", transaction.Type);
        command.Parameters.AddWithValue("category", transaction.Category.Trim());
        command.Parameters.AddWithValue("amount", transaction.Amount);
        command.Parameters.AddWithValue("date", transaction.Date);

        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();

        var result = new
        {
            id = reader.GetInt64(0),
            type = reader.GetString(1),
            category = reader.GetString(2),
            amount = reader.GetDecimal(3),
            date = reader.GetDateTime(4).ToString("yyyy-MM-dd")
        };

        return Results.Json(result, statusCode: 201);
    }
    catch (NpgsqlException)
    {
        return Results.Problem(statusCode: 503, detail: "Could not save transaction.");
    }
});

var listenUrl = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://0.0.0.0:8000";
app.Run(listenUrl);

// =====================================================
// HELPERS
// =====================================================

static IResult? Validate(object model)
{
    var results = new List<ValidationResult>();
    var context = new ValidationContext(model);

    if (Validator.TryValidateObject(model, context, results, validateAllProperties: true))
    {
        return null;
    }

    return Results.Problem(statusCode: 422, detail: string.Join(" ", results.Select(r => r.ErrorMessage)));
}

static string ToNpgsqlConnectionString(string databaseUrl)
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);

    var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
    var sslModeValue = query.TryGetValue("sslmode", out var sslValues) ? sslValues.ToString() : "require";
    var sslMode = sslModeValue switch
    {
        "require" => "Require",
        "disable" => "Disable",
        "prefer" => "Prefer",
        _ => "Require"
    };

    var builder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.IsDefaultPort ? 5432 : uri.Port,
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "",
        Database = uri.AbsolutePath.TrimStart('/'),
        SslMode = Enum.Parse<SslMode>(sslMode)
    };

    return builder.ConnectionString;
}

static async Task<string> GenerateUniqueUsernameAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string baseName)
{
    var cleaned = Regex.Replace(baseName, "[^a-zA-Z0-9]", "");
    if (string.IsNullOrEmpty(cleaned))
    {
        cleaned = "user";
    }

    var candidate = cleaned;
    var suffix = 1;

    while (true)
    {
        await using var command = new NpgsqlCommand(
            "SELECT id FROM users WHERE LOWER(username) = LOWER(@candidate) LIMIT 1", connection, transaction);
        command.Parameters.AddWithValue("candidate", candidate);

        if (await command.ExecuteScalarAsync() is null)
        {
            return candidate;
        }

        suffix += 1;
        candidate = $"{cleaned}{suffix}";
    }
}

static async Task InitializeDatabaseAsync(string connectionString)
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    string[] statements =
    [
        """
        CREATE TABLE IF NOT EXISTS users (
            id BIGSERIAL PRIMARY KEY,
            username TEXT NOT NULL,
            email TEXT NOT NULL UNIQUE,
            password_hash TEXT NOT NULL,
            created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
        )
        """,
        """
        CREATE TABLE IF NOT EXISTS transactions (
            id BIGSERIAL PRIMARY KEY,
            user_id BIGINT,
            type TEXT NOT NULL CHECK (type IN ('income', 'expense')),
            category TEXT NOT NULL,
            amount NUMERIC(12, 2) NOT NULL CHECK (amount > 0),
            transaction_date DATE NOT NULL,
            created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            CONSTRAINT fk_transactions_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
        )
        """,
        "ALTER TABLE transactions ADD COLUMN IF NOT EXISTS user_id BIGINT",
        """
        DO $$
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_transactions_user') THEN
                ALTER TABLE transactions
                ADD CONSTRAINT fk_transactions_user
                FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE;
            END IF;
        END
        $$;
        """,
        "CREATE UNIQUE INDEX IF NOT EXISTS users_username_unique ON users (LOWER(username))",
        "ALTER TABLE users ALTER COLUMN password_hash DROP NOT NULL",
        "ALTER TABLE users ADD COLUMN IF NOT EXISTS google_sub TEXT",
        "CREATE UNIQUE INDEX IF NOT EXISTS users_google_sub_unique ON users (google_sub) WHERE google_sub IS NOT NULL"
    ];

    foreach (var sql in statements)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
