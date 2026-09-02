using DotNetEnv;
using FinanceDashboardApi.Common;
using FinanceDashboardApi.Data;
using FinanceDashboardApi.Middleware;
using FinanceDashboardApi.Repositories;
using FinanceDashboardApi.Repositories.Interfaces;
using FinanceDashboardApi.Services;
using FinanceDashboardApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// ENVIRONMENT
// =====================================================

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var authSecret = Environment.GetEnvironmentVariable("AUTH_SECRET");
var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");

if (string.IsNullOrEmpty(authSecret))
{
    if (builder.Environment.IsDevelopment())
    {
        authSecret = "finance-dashboard-secret-change-this";
        Console.WriteLine("WARNING: AUTH_SECRET is not set — using an insecure development default.");
    }
    else
    {
        // Failing fast beats silently signing every token with a secret an
        // attacker can read straight out of source control.
        throw new InvalidOperationException("AUTH_SECRET must be set outside Development.");
    }
}

var npgsqlConnectionString = databaseUrl is null ? null : ConnectionStringBuilder.FromDatabaseUrl(databaseUrl);

// =====================================================
// SERVICES
// =====================================================

// Registered unconditionally so DI can always resolve AppDbContext — if
// DATABASE_URL is missing, any endpoint that actually touches the DB will
// fail loudly (caught by ExceptionHandlingMiddleware) rather than the app
// failing to start. /health and / stay usable either way.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(npgsqlConnectionString ?? "Host=unconfigured"));

builder.Services.AddSingleton(new AuthOptions { AuthSecret = authSecret, GoogleClientId = googleClientId });
builder.Services.AddSingleton<ITokenService>(new TokenService(authSecret));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();

builder.Services
    .AddAuthentication(TokenAuthenticationHandler.SchemeName)
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TokenAuthenticationHandler>(
        TokenAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Preserve the original API's 422-for-validation-errors contract
        // instead of ASP.NET Core's [ApiController] default of 400.
        options.InvalidModelStateResponseFactory = context =>
        {
            var messages = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);

            return new ObjectResult(new ProblemDetails
            {
                Status = 422,
                Detail = string.Join(" ", messages)
            })
            { StatusCode = 422 };
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Finance Dashboard API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "opaque",
        In = ParameterLocation.Header,
        Description = "Paste just the token returned from /api/login or /api/register — no \"Bearer \" prefix needed."
    });

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer"), new List<string>() }
    });
});

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                  ?? ["http://localhost:4200", "http://127.0.0.1:4200"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(corsOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials());
});

var app = builder.Build();

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// =====================================================
// DATABASE INITIALIZATION
// =====================================================

if (npgsqlConnectionString is not null)
{
    try
    {
        await SchemaBootstrapper.RunAsync(npgsqlConnectionString);
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

var listenUrl = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://0.0.0.0:8007";
app.Run(listenUrl);
