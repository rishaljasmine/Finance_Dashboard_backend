# FinanceDashboard

## Neon PostgreSQL backend

The API in `backend-dotnet/Program.cs` (ASP.NET Core 8, minimal API) stores
transactions in Neon PostgreSQL. Do not put the Neon connection string in
Angular or commit it to source control.

1. Create a project at [Neon](https://neon.tech) and copy its pooled connection string.
2. Open a terminal in `backend-dotnet` and copy `.env.example` to `.env`, then
   replace `DATABASE_URL` with the Neon URL.
3. Start the API:

	```bash
	dotnet run
	```

The app creates/migrates the `users` and `transactions` tables itself on
startup — no manual SQL needed.

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
