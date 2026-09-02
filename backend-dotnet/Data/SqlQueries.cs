namespace FinanceDashboardApi.Data;

/// <summary>
/// Every raw SQL string the API sends to Postgres, kept in one place so
/// Program.cs stays about routing/logic rather than query text.
/// </summary>
public static class SqlQueries
{
    // =====================================================
    // HEALTH
    // =====================================================

    public const string HealthCheck = "SELECT 1";


    // =====================================================
    // USERS
    // =====================================================

    public const string FindUserIdByUsername =
        "SELECT id FROM users WHERE LOWER(username) = LOWER(@username) LIMIT 1";

    public const string FindUserIdByEmail =
        "SELECT id FROM users WHERE LOWER(email) = LOWER(@email) LIMIT 1";

    public const string InsertUser =
        """
        INSERT INTO users (username, email, password_hash)
        VALUES (@username, @email, @passwordHash)
        RETURNING id, username, email
        """;

    public const string FindUserByUsernameForLogin =
        "SELECT id, username, email, password_hash FROM users WHERE LOWER(username) = LOWER(@value) LIMIT 1";

    public const string FindUserByEmailForLogin =
        "SELECT id, username, email, password_hash FROM users WHERE LOWER(email) = LOWER(@value) LIMIT 1";

    public const string FindUserByGoogleSub =
        "SELECT id, username, email FROM users WHERE google_sub = @sub LIMIT 1";

    public const string FindUserByEmail =
        "SELECT id, username, email FROM users WHERE LOWER(email) = LOWER(@email) LIMIT 1";

    public const string LinkGoogleSubToUser =
        "UPDATE users SET google_sub = @sub WHERE id = @id";

    public const string InsertGoogleUser =
        """
        INSERT INTO users (username, email, password_hash, google_sub)
        VALUES (@username, @email, NULL, @sub)
        RETURNING id, username, email
        """;

    public const string GetUserById =
        "SELECT id, username, email FROM users WHERE id = @id";

    public const string FindUserIdByUsernameCandidate =
        "SELECT id FROM users WHERE LOWER(username) = LOWER(@candidate) LIMIT 1";


    // =====================================================
    // TRANSACTIONS
    // =====================================================

    public const string GetTransactionYears =
        """
        SELECT DISTINCT EXTRACT(YEAR FROM transaction_date)::int AS year
        FROM transactions
        WHERE user_id = @userId
        ORDER BY year DESC
        """;

    public const string GetTransactionsForYear =
        """
        SELECT id, type, category, amount, transaction_date
        FROM transactions
        WHERE user_id = @userId
        AND EXTRACT(YEAR FROM transaction_date) = @year
        ORDER BY transaction_date DESC, id DESC
        """;

    public const string InsertTransaction =
        """
        INSERT INTO transactions (user_id, type, category, amount, transaction_date)
        VALUES (@userId, @type, @category, @amount, @date)
        RETURNING id, type, category, amount, transaction_date
        """;


    // =====================================================
    // SCHEMA (run once at startup — create/migrate tables)
    // =====================================================

    public static readonly string[] SchemaStatements =
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
}
