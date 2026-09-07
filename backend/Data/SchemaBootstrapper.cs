using Npgsql;

namespace FinanceDashboardApi.Data;

/// <summary>
/// Runs idempotent DDL at startup so the schema is created/migrated without a
/// separate `dotnet ef database update` step. Kept as raw SQL (rather than EF
/// Migrations) so behavior stays identical to the previous version: safe to
/// run on every boot, no migrations history table to manage.
/// </summary>
public static class SchemaBootstrapper
{
    private static readonly string[] Statements =
    [
        """
        CREATE TABLE IF NOT EXISTS users (
            id BIGSERIAL PRIMARY KEY,
            username TEXT NOT NULL,
            email TEXT NOT NULL UNIQUE,
            password_hash TEXT,
            google_sub TEXT,
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
        """
        CREATE TABLE IF NOT EXISTS files (
            id BIGSERIAL PRIMARY KEY,
            user_id BIGINT NOT NULL,
            transaction_id BIGINT,
            original_file_name TEXT NOT NULL,
            stored_file_name TEXT NOT NULL,
            content_type TEXT NOT NULL,
            size_bytes BIGINT NOT NULL,
            uploaded_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            CONSTRAINT fk_files_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
            CONSTRAINT fk_files_transaction FOREIGN KEY (transaction_id) REFERENCES transactions(id) ON DELETE CASCADE
        )
        """,
        // Defensive: fills in transaction_id for a files table that was already
        // created by an earlier boot, before this column existed.
        "ALTER TABLE files ADD COLUMN IF NOT EXISTS transaction_id BIGINT REFERENCES transactions(id) ON DELETE CASCADE",
        "CREATE UNIQUE INDEX IF NOT EXISTS users_username_unique ON users (LOWER(username))",
        "CREATE UNIQUE INDEX IF NOT EXISTS users_google_sub_unique ON users (google_sub) WHERE google_sub IS NOT NULL"
    ];

    public static async Task RunAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        foreach (var sql in Statements)
        {
            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}
