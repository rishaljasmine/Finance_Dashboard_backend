# FinanceDashboard

## Neon PostgreSQL backend

The API in `backend/main.py` stores transactions in Neon PostgreSQL. Do not put
the Neon connection string in Angular or commit it to source control.

1. Create a project at [Neon](https://neon.tech) and copy its pooled connection string.
2. Open a terminal in `backend` and install the Python dependencies:

	```bash
	python -m pip install -r requirements.txt
	```

3. Copy `.env.example` to `.env` and replace `DATABASE_URL` with the Neon URL.
4. Start the API:

	```bash
	uvicorn main:app --reload --port 8000
	```

Run this SQL once in the Neon SQL Editor to create the `transactions` table:

```sql
CREATE TABLE IF NOT EXISTS transactions (
	id BIGSERIAL PRIMARY KEY,
	type TEXT NOT NULL CHECK (type IN ('income', 'expense')),
	category TEXT NOT NULL,
	amount NUMERIC(12, 2) NOT NULL CHECK (amount > 0),
	transaction_date DATE NOT NULL,
	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS users (
	id BIGSERIAL PRIMARY KEY,
	username TEXT NOT NULL,
	email TEXT NOT NULL UNIQUE,
	password_hash TEXT NOT NULL,
	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

Then check the connection at
`http://127.0.0.1:8000/api/health`. The API supports `GET /api/transactions?year=2025`
and `POST /api/transactions`.

Example request body:

```json
{
  "type": "expense",
  "category": "Food",
  "amount": 2500,
  "date": "2025-06-20"
}
```

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 22.1.5.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
