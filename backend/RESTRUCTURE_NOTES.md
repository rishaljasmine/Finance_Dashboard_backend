# Restructure notes

Rebuilt from a single-file minimal-API `Program.cs` into a layered project:

```
Controllers/   → HTTP concerns only (routes, [Authorize], status codes)
Services/      → business logic, returns ServiceResult<T> (no HTTP types)
Repositories/  → EF Core data access (AppDbContext)
Entities/      → EF Core models mapped to the existing snake_case schema
DTOs/          → request/response contracts, separate from entities
Data/          → AppDbContext + SchemaBootstrapper (idempotent DDL, unchanged behavior)
Middleware/    → TokenAuthenticationHandler, ExceptionHandlingMiddleware
Common/        → ServiceResult<T>, AuthOptions, connection-string helper
```

## What changed functionally

- **Raw Npgsql → EF Core** (`AppDbContext`). Schema creation stays raw idempotent SQL
  (`SchemaBootstrapper`) rather than EF Migrations, so an already-deployed DB needs no
  migration step and boots exactly as before.
- **Manual per-endpoint auth checks → `[Authorize]`.** Every protected handler used to
  re-parse `Authorization: Bearer <token>` by hand. Now `TokenAuthenticationHandler` plugs
  the same token format into ASP.NET Core's standard auth pipeline once; controllers just
  add `[Authorize]`. Same 401 JSON shape (`{ "detail": "..." }`) is preserved.
- **`AUTH_SECRET` fallback now fails fast.** Previously a missing `AUTH_SECRET` silently
  fell back to a hardcoded string in *any* environment. Now that fallback only applies in
  `Development`; anywhere else, startup throws instead of quietly signing tokens with a
  secret sitting in source control.
- **Register endpoint hardened against a race.** The exists-check-then-insert had a TOCTOU
  window under concurrent signups. Insert now also catches a Postgres unique-violation
  (`23505`) as a fallback 409, instead of risking an unhandled 500.
- **CORS origins moved to `appsettings.json` (`Cors:AllowedOrigins`)** instead of being
  hardcoded in `Program.cs`.
- **Validation still returns 422**, not ASP.NET Core's default 400 for `[ApiController]` —
  overridden via `InvalidModelStateResponseFactory` to keep the existing frontend contract.
- All routes, request/response shapes, and status codes are otherwise unchanged.

## Not changed / left as-is

- No new endpoints were added (e.g. no transaction update/delete) — out of scope for a
  restructure; flag if you want those.
- Token format is still the original custom `base64url(payload).hex(hmac)` scheme, not a JWT.

## Build

NuGet access wasn't available in the sandbox that produced this, so it has **not** been
`dotnet build`-tested. Packages added: `Microsoft.EntityFrameworkCore` and
`Npgsql.EntityFrameworkCore.PostgreSQL` (both `8.0.10`), replacing the old direct
`Npgsql`/`Npgsql.DependencyInjection` references. Run:

```
dotnet restore
dotnet build
```

and fix up anything the compiler flags before deploying.
