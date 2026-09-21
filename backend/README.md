# Finance Dashboard API

ASP.NET Core 8 REST API backed by Supabase PostgreSQL. It is the only
component that authenticates users and touches the database. See the
[root README](../README.md) for the full project setup.

## Run

```bash
cp .env.example .env    # set DATABASE_URL (Supabase session pooler) and AUTH_SECRET
dotnet run              # http://localhost:8002
```

- Swagger: http://localhost:8002/swagger/index.html
- Health check: http://localhost:8002/api/health

The schema (`users`, `transactions`, uploaded files) is created and migrated
automatically on startup.

## Structure

| Folder          | Responsibility                                  |
| --------------- | ----------------------------------------------- |
| `Controllers/`  | HTTP endpoints (auth, transactions, dashboard, files) |
| `Services/`     | Business logic, token handling, file storage    |
| `Repositories/` | Data access; SQL lives in `Data/SqlQueries.cs`  |
| `Entities/`, `DTOs/` | Persistence models and request/response shapes |
| `Middleware/`   | Cookie/token authentication handler             |

See [RESTRUCTURE_NOTES.md](RESTRUCTURE_NOTES.md) for design notes.

Uploaded files are stored under `Storage/` (gitignored). Never commit `.env`.
