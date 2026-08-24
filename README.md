# Microservices Auth Solution

ASP.NET Core 8 microservice architecture with JWT login authentication and
role-based authorization (**Admin** / **User**), backed by SQL Server, behind
an API Gateway.

## Solution Layout

```
MicroservicesAuthSolution/
├── src/
│   ├── ApiGateway/            YARP reverse-proxy gateway (port 5000)
│   ├── AuthService/           Registration, login, JWT issuing (port 5001)
│   │   ├── AuthService.Api
│   │   ├── AuthService.Application
│   │   ├── AuthService.Domain
│   │   └── AuthService.Infrastructure
│   ├── UserService/           Admin/User profile endpoints (port 5002)
│   │   ├── UserService.Api
│   │   ├── UserService.Application
│   │   ├── UserService.Domain
│   │   └── UserService.Infrastructure
│   └── Shared/
│       └── Shared.Auth        Shared JWT config/validation extensions
├── docker-compose.yml
├── setup.ps1                  One-time solution/project bootstrap script
└── README.md
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (any of: local SQL Server, SQL Server Express/LocalDB, or the
  SQL Server container in `docker-compose.yml`)
- (Optional) Docker Desktop, if you want to run everything in containers

## 1. First-Time Setup

After extracting this folder to `C:\Users\AyushPanchal\Desktop\MicroservicesAuthSolution`,
open **PowerShell** in that folder and run:

```powershell
.\setup.ps1
```

This will:
1. Create `MicroservicesAuthSolution.sln`
2. Add every `.csproj` in `src/` to the solution
3. `dotnet restore` all packages
4. Install/update the `dotnet-ef` CLI tool (needed for migrations)

> If `setup.ps1` is blocked by execution policy, run:
> `powershell -ExecutionPolicy Bypass -File .\setup.ps1`

## 2. Database Setup (EF Core Migrations)

Each service owns its own database (`AuthDb` and `UserDb`). Create the
initial migrations and apply them (they'll also auto-apply on startup via
`dbContext.Database.Migrate()`, but creating them once locally is required
first):

```powershell
# AuthService -> AuthDb
dotnet ef migrations add InitialCreate `
  --project src/AuthService/AuthService.Infrastructure `
  --startup-project src/AuthService/AuthService.Api

dotnet ef database update `
  --project src/AuthService/AuthService.Infrastructure `
  --startup-project src/AuthService/AuthService.Api

# UserService -> UserDb
dotnet ef migrations add InitialCreate `
  --project src/UserService/UserService.Infrastructure `
  --startup-project src/UserService/UserService.Api

dotnet ef database update `
  --project src/UserService/UserService.Infrastructure `
  --startup-project src/UserService/UserService.Api
```

Make sure the connection string in each service's `appsettings.json`
points to a SQL Server instance you can reach (default assumes
`localhost,1433` with `sa` / `YourStrong!Passw0rd` — change this).

## 3. Configuration You MUST Review

`src/AuthService/AuthService.Api/appsettings.json` and
`src/UserService/UserService.Api/appsettings.json` both contain a
`JwtSettings` section. **`SecretKey` must be identical in both files**
(UserService validates tokens issued by AuthService using the same key).
Replace the placeholder with your own long random secret, and never commit
real secrets — use `dotnet user-secrets` or environment variables instead:

```powershell
cd src/AuthService/AuthService.Api
dotnet user-secrets init
dotnet user-secrets set "JwtSettings:SecretKey" "your-long-random-secret-here"
```

`AuthService`'s `appsettings.json` also has a `BootstrapAdmin` section — this
seeds one Admin account on first startup so you have a way in. Change the
email/password before running in anything but local dev.

## 4. Running Locally (without Docker)

Open three terminals from the solution root:

```powershell
# Terminal 1
dotnet run --project src/AuthService/AuthService.Api

# Terminal 2
dotnet run --project src/UserService/UserService.Api

# Terminal 3
dotnet run --project src/ApiGateway
```

- AuthService Swagger: `https://localhost:5001/swagger` (or the port shown in console)
- UserService Swagger: `https://localhost:5002/swagger`
- Gateway: `http://localhost:5000` routes `/api/auth/*` → AuthService, `/api/users/*` → UserService

## 5. Running with Docker Compose

```powershell
docker-compose up --build
```

This starts SQL Server, AuthService, UserService, and the API Gateway together.
- Gateway: `http://localhost:5000`
- AuthService direct: `http://localhost:5001`
- UserService direct: `http://localhost:5002`
- SQL Server: `localhost,1433` (sa / YourStrong!Passw0rd)

> The Docker version applies migrations automatically on container startup
> (`Database.Migrate()` in each `Program.cs`), so you don't need to run the
> `dotnet ef` commands above when using Compose — just make sure the
> `Migrations` folders exist (i.e., you ran step 2 at least once so the
> migration files are generated and included in the image).

## 6. Sample Requests

### Register a normal user
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"fullName":"Jane Doe","email":"jane@example.com","password":"P@ssw0rd123"}'
```

### Log in (as the seeded bootstrap admin)
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"Admin@12345"}'
```
Response includes `accessToken` and `refreshToken`.

### Call an authenticated endpoint
```bash
curl http://localhost:5000/api/users/me \
  -H "Authorization: Bearer <accessToken>"
```

### Admin-only: list all users
```bash
curl http://localhost:5000/api/users \
  -H "Authorization: Bearer <admin-accessToken>"
```

### Admin-only: create another Admin
```bash
curl -X POST http://localhost:5000/api/auth/register-admin \
  -H "Authorization: Bearer <admin-accessToken>" \
  -H "Content-Type: application/json" \
  -d '{"fullName":"Second Admin","email":"admin2@example.com","password":"P@ssw0rd123"}'
```

### Refresh an expired access token
```bash
curl -X POST http://localhost:5000/api/auth/refresh-token \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"<refreshToken>"}'
```

## Endpoint Summary

| Service | Method | Route | Access |
|---|---|---|---|
| Auth | POST | `/api/auth/register` | Public — registers as `User` |
| Auth | POST | `/api/auth/register-admin` | Admin only |
| Auth | POST | `/api/auth/login` | Public |
| Auth | POST | `/api/auth/refresh-token` | Public (valid refresh token) |
| Auth | POST | `/api/auth/logout` | Authenticated |
| User | GET | `/api/users/me` | Authenticated (User/Admin) |
| User | PUT | `/api/users/me` | Authenticated (User/Admin) |
| User | GET | `/api/users` | Admin only |
| User | GET | `/api/users/{id}` | Admin only |
| User | DELETE | `/api/users/{id}` | Admin only |
| User | PATCH | `/api/users/{id}/role` | Admin only |

## Notes / Next Steps

- **Password reset / email confirmation** are not included — add them via
  ASP.NET Core Identity's existing token providers if needed.
- **Role sync**: `UsersController.ChangeRole` only updates UserService's own
  denormalized `Role` field. To fully change a user's role you should also
  call AuthService (extend it with an admin "change role" endpoint that
  updates `AspNetUserRoles`), or introduce a message broker (RabbitMQ +
  MassTransit) to keep both services eventually consistent.
- **HTTPS**: `RequireHttpsMetadata = false` is set for local development
  convenience. Set it to `true` and terminate TLS properly before
  deploying to production.
- **Production secrets**: never leave `SecretKey`, `SA_PASSWORD`, or
  `BootstrapAdmin` credentials as the placeholder values in this repo.
