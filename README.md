# FixFlow AI

Intelligent Home Service Management Platform.

## Tech stack

| Area | Technologies |
| --- | --- |
| Web | React, TypeScript, Vite, React Router, Axios, Context API, Zustand, Tailwind CSS, Lucide React |
| Mobile | Flutter, Dart, Riverpod, Dio, flutter_secure_storage, image_picker, geolocator |
| Backend | C#, ASP.NET Core Web API, .NET 8, EF Core, Npgsql, JWT, FluentValidation, Swagger |
| Database | PostgreSQL, pgAdmin 4, database name `FixFlow` |
| Agents | .NET orchestration, LLM adapter, structured JSON, tool calling, human approval |
| Testing | xUnit, Moq, React Testing Library, Vitest, Flutter Test |
| Source control | Git, GitHub, GitHub Actions |

## Local database

Create a PostgreSQL database named `FixFlow` in pgAdmin 4.

- Host: `localhost`
- Port: `5432`
- Username: `postgres`
- Put the password only in `.env` and `backend/FixFlow.Api/appsettings.Local.json` (both are gitignored)

Production should use environment variables or .NET user secrets:

```powershell
dotnet user-secrets init --project backend/FixFlow.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=...;Database=FixFlow;Username=...;Password=..." --project backend/FixFlow.Api
```

Apply the schema:

```powershell
dotnet ef database update --project backend/FixFlow.Infrastructure --startup-project backend/FixFlow.Api --context FixFlowDbContext
```

## Run

```powershell
dotnet run --project backend/FixFlow.Api
```

Swagger: `http://localhost:5080/swagger`

```powershell
cd web
npm install
npm run dev
```

```powershell
cd mobile
flutter pub get
flutter run
```

Demo login: `admin@fixflow.local` / `Admin123!`

## Tests

```powershell
dotnet test FixFlow.sln
cd web; npm test
cd mobile; flutter test
```
