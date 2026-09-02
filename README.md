# Finance Dashboard — Backend

ASP.NET Core 8 minimal API backend for the [Finance Dashboard](https://github.com/rishaljasmine/Finance_Dashboard) app. Handles authentication (including Google sign-in) and transaction storage against a Postgres database.

## Tech stack

- **ASP.NET Core 8** — minimal API (no MVC controllers)
- **Npgsql** — Postgres driver
- **Google.Apis.Auth** — verifies Google Sign-In ID tokens
- **BouncyCastle.Cryptography** — scrypt password hashing
- **DotNetEnv** — loads config from a local `.env` file

## Setup

1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Create a Postgres database — [Neon](https://neon.tech) works well and is free for small projects.
3. Copy `.env.example` to `.env` and fill in:

   ```
   DATABASE_URL=postgresql://user:password@host/dbname?sslmode=require
   GOOGLE_CLIENT_ID=            # optional — leave blank to disable Google sign-in
   AUTH_SECRET=                 # optional — a long random string; has a dev fallback if omitted
   ```

4. Run it:

   ```bash
   dotnet run
   ```

The `users` and `transactions` tables are created (and migrated) automatically on startup — no manual SQL needed. The API listens on `http://0.0.0.0:8000` by default.

Check it's up at `http://127.0.0.1:8000/api/health`.

## API

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/health` | — | DB connectivity check |
| POST | `/api/register` | — | Create an account |
| POST | `/api/login` / `/api/auth/login` | — | Log in with username/email + password |
| POST | `/api/auth/google` | — | Log in with a Google ID token |
| GET | `/api/me` | Bearer token | Current user |
| GET | `/api/transactions/years` | Bearer token | Years that have transaction data |
| GET | `/api/transactions?year=YYYY` | Bearer token | Transactions for a year |
| POST | `/api/transactions` | Bearer token | Create a transaction |

Auth uses a signed bearer token (`Authorization: Bearer <token>`) returned from register/login — not JWT, a simpler HMAC-signed payload, but functionally equivalent for this app's needs.

## Project structure

```
Program.cs            Route definitions, app startup, request handlers
Data/SqlQueries.cs     Every SQL statement the API runs, in one place
Models/                Request DTOs (RegisterRequest, TransactionCreate, ...)
Services/
  PasswordHasher.cs    scrypt hashing/verification
  TokenService.cs      Auth token create/verify
  Base64Url.cs          URL-safe base64 helper
```

## Notes

- Password hashing (scrypt, N=16384/r=8/p=1) is byte-for-byte compatible with this project's earlier Python/FastAPI backend, so accounts created under either backend work with both.
- `AUTH_SECRET` signs login tokens. It has a hardcoded development fallback if left unset — set a real one before deploying anywhere public.
