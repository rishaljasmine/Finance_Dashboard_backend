# Finance Dashboard

A personal finance dashboard: an Angular app rendered on the server (SSR), a
React sidebar microfrontend, and an ASP.NET Core API backed by Supabase
PostgreSQL.

```
Browser -> Angular SSR (Node, :4200) -> .NET REST API (:8002) -> PostgreSQL
```

## Repository layout

| Folder         | What it is                                             | Stack                       |
| -------------- | ------------------------------------------------------ | --------------------------- |
| `backend/`     | REST API: auth, transactions, dashboard, file uploads  | ASP.NET Core 8, PostgreSQL  |
| `frontend/`    | Dashboard UI, server-side rendered                     | Angular 22, Express, Tailwind |
| `sidebar-mfe/` | `<finova-sidebar>` custom element used by the frontend | React 19, Vite, Tailwind    |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- A current LTS [Node.js](https://nodejs.org) (the version Angular 22 supports) and npm
- A [Supabase](https://supabase.com) project (or any PostgreSQL database)

## Quick start

Run the API and the frontend in two terminals, starting from the repo root.

### 1. Backend (API on http://localhost:8002)

```bash
cd backend
cp .env.example .env        # then edit .env (see below)
dotnet run
```

Fill in `backend/.env`:

| Variable           | Purpose                                                                                     |
| ------------------ | ------------------------------------------------------------------------------------------- |
| `DATABASE_URL`     | Supabase **session pooler** connection string (not the direct connection, which is IPv6-only) |
| `AUTH_SECRET`      | Signs session tokens. Use a long random string; required outside Development                |
| `GOOGLE_CLIENT_ID` | Optional, for Google sign-in                                                                |
| `COOKIE_DOMAIN`    | Optional, only when the frontend and API sit on different subdomains in production           |

Special characters in the database password must be percent-encoded
(`@` -> `%40`, `#` -> `%23`). Never commit `.env`.

The API creates and migrates its `users`, `transactions` and file tables on
startup, so no manual SQL is needed.

- Swagger: http://localhost:8002/swagger/index.html
- Health check: http://localhost:8002/api/health

### 2. Frontend (SSR on http://localhost:4200)

The frontend is served in production mode by a Node/Express server, so it is a
**build step followed by a serve step**:

```bash
cd frontend
npm install
npm run build                        # production SSR build into dist/
npm run serve:ssr:finance-dashboard  # start the SSR server on :4200
```

Open http://localhost:4200. Re-run `npm run build` after any frontend change
before serving again.

For day-to-day development with live reload (client-side only, no SSR):

```bash
cd frontend
npm start                            # ng serve on :4200
```

Optional environment variables for the SSR server: `PORT` (default `4200`) and
`API_URL` (default `http://127.0.0.1:8002`).

> Use **one hostname** for both apps (`localhost` everywhere, or `127.0.0.1`
> everywhere). The session cookie is host-scoped, so mixing them breaks login.

## Command reference

### `backend/`

| Command                 | What it does                            |
| ----------------------- | --------------------------------------- |
| `dotnet run`            | Run the API on http://localhost:8002    |
| `dotnet build`          | Compile the API                         |
| `dotnet publish -c Release` | Produce a deployable build          |

### `frontend/`

| Command                                | What it does                                                   |
| -------------------------------------- | -------------------------------------------------------------- |
| `npm install`                          | Install dependencies                                           |
| `npm run build`                        | Production build (browser bundle + SSR server) into `dist/`    |
| `npm run serve:ssr:finance-dashboard`  | Start the built SSR server (`node dist/finance-dashboard/server/server.mjs`) |
| `npm start`                            | Dev server with live reload (`ng serve`)                       |
| `npm run watch`                        | Rebuild on change (development configuration)                  |
| `npm test`                             | Unit tests with coverage (Vitest via `ng test`)                |

### `sidebar-mfe/`

| Command                 | What it does                                                        |
| ----------------------- | ------------------------------------------------------------------- |
| `npm install`           | Install dependencies                                                |
| `npm run dev`           | Run the sidebar standalone with Vite                                |
| `npm run build`         | Type-check and build the standalone app into `dist/`                |
| `npm run build:element` | Build the `<finova-sidebar>` custom element into `dist-element/`    |
| `npm run preview`       | Preview the standalone build                                        |

The frontend loads the sidebar from `frontend/public/mfe/`. After changing the
sidebar, rebuild it and copy the output across, then rebuild the frontend:

```bash
cd sidebar-mfe
npm run build:element
cp dist-element/finova-sidebar.js dist-element/sidebar-mfe.css ../frontend/public/mfe/
cd ../frontend && npm run build
```

## How authentication works

- Sessions are an **HttpOnly cookie** (`fd_session`) set by the .NET API on
  login. Nothing auth-related lives in `localStorage`.
- The SSR server forwards the browser's cookie to the API when it renders a
  page; the browser sends the cookie straight to the API for REST calls
  (create transaction, upload file, ...).
- Every route, including `/dashboard`, is rendered on the server and hydrated in
  the browser. The .NET API remains the only place that authenticates,
  authorises and touches PostgreSQL.
- Serve everything over HTTPS in production; the cookie is `Secure`
  automatically when the API sees HTTPS.

Verify SSR (replace `<token>` with a valid session cookie value):

```bash
curl -s -H "Cookie: fd_session=<token>" http://localhost:4200/dashboard
# full dashboard markup, including ng-server-context="ssr"

curl -i http://localhost:4200/dashboard
# 302 Location: /login   (no cookie)
```

## API examples

The API supports `GET /api/transactions?year=2025` and `POST /api/transactions`:

```json
{
  "type": "expense",
  "category": "Food",
  "amount": 2500,
  "date": "2025-06-20"
}
```

See Swagger for the full list of endpoints (auth, transactions, dashboard,
files).

## Further reading

- [Angular CLI reference](https://angular.dev/tools/cli)
- [`backend/README.md`](backend/README.md) for backend-only notes
