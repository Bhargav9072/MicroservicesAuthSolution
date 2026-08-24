# MicroservicesAuthSolution - Technical Documentation

## 1. Overview
This solution is a .NET 8 microservices-based authentication and user-management system with three services:

- **ApiGateway**: Reverse proxy (YARP) and centralized Swagger entrypoint
- **AuthService**: Identity, login, JWT/refresh token generation, logout, token audit
- **UserService**: User registration orchestration and profile management/update

## 2. Architecture

### 2.1 Service Responsibilities
- **UserService**
  - Public user registration (`POST /api/users/register`)
  - User profile CRUD/update (`/api/users/*`)
- **AuthService**
  - Credential creation endpoint used by UserService (`POST /api/auth/register`)
  - Login (`POST /api/auth/login`)
  - Refresh token (`POST /api/auth/refresh-token`)
  - Logout (`POST /api/auth/logout`)
  - Admin registration (`POST /api/auth/register-admin`)
- **ApiGateway**
  - Proxies `/api/auth/*` to AuthService
  - Proxies `/api/users/*` to UserService
  - Hosts Swagger UI and proxies service Swagger docs

### 2.2 Security
- JWT Bearer auth is shared through `Shared.Auth` (`JwtAuthExtensions`)
- Common JWT settings:
  - `Issuer`: `AuthService`
  - `Audience`: `MicroservicesClients`
- Role-based authorization:
  - Admin-only endpoints in UserService and AuthService use `[Authorize(Roles = "Admin")]`

## 3. Runtime Endpoints
Based on launch settings:

- **ApiGateway**: `https://localhost:59566` / `http://localhost:59570`
- **AuthService**: `https://localhost:59567` / `http://localhost:59568`
- **UserService**: `https://localhost:59565` / `http://localhost:59569`

Swagger:
- Gateway: `https://localhost:59566/swagger`
- Auth direct: `https://localhost:59567/swagger`
- User direct: `https://localhost:59565/swagger`

## 4. Key API Flows

### 4.1 Registration Flow (UserService-owned)
1. Client calls `POST /api/users/register`
2. UserService calls AuthService `POST /api/auth/register`
3. AuthService creates identity user and returns `userId`, `email`, `fullName`
4. UserService stores profile in `UserDb.dbo.UserProfiles`

### 4.2 Login + Token Flow (AuthService-owned)
1. Client calls `POST /api/auth/login`
2. AuthService validates credentials
3. AuthService returns access + refresh token
4. AuthService stores:
   - refresh token in `AuthDb.dbo.RefreshTokens`
   - access token audit in `AuthDb.dbo.AccessTokenAudits`

### 4.3 Refresh and Logout
- Refresh rotates refresh token and issues a new access token
- Logout revokes:
  - active bearer token audit row
  - provided refresh token row

## 5. SQL Database Design

## 5.1 Databases
- `AuthDb`
- `UserDb`

## 5.2 AuthDb Tables
- `AspNetUsers`
- `AspNetRoles`
- `AspNetUserRoles`
- `AspNetUserClaims`
- `AspNetRoleClaims`
- `AspNetUserLogins`
- `AspNetUserTokens`
- `RefreshTokens`
- `AccessTokenAudits`

## 5.3 UserDb Tables
- `UserProfiles`

## 6. Full SQL Script (from scratch, INT IDs)

```sql
USE [master];
GO

IF DB_ID(N'UserDb') IS NOT NULL
BEGIN
	ALTER DATABASE [UserDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
	DROP DATABASE [UserDb];
END
GO

IF DB_ID(N'AuthDb') IS NOT NULL
BEGIN
	ALTER DATABASE [AuthDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
	DROP DATABASE [AuthDb];
END
GO

CREATE DATABASE [AuthDb];
GO
CREATE DATABASE [UserDb];
GO

USE [AuthDb];
GO

CREATE TABLE [dbo].[AspNetRoles]
(
	[Id]               INT IDENTITY(1,1) NOT NULL,
	[Name]             NVARCHAR(256) NULL,
	[NormalizedName]   NVARCHAR(256) NULL,
	[ConcurrencyStamp] NVARCHAR(MAX) NULL,
	CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [dbo].[AspNetUsers]
(
	[Id]                    INT IDENTITY(1,1) NOT NULL,
	[FullName]              NVARCHAR(200) NOT NULL CONSTRAINT [DF_AspNetUsers_FullName] DEFAULT (N''),
	[CreatedAt]             DATETIME2 NOT NULL CONSTRAINT [DF_AspNetUsers_CreatedAt] DEFAULT (SYSUTCDATETIME()),
	[IsActive]              BIT NOT NULL CONSTRAINT [DF_AspNetUsers_IsActive] DEFAULT (1),
	[UserName]              NVARCHAR(256) NULL,
	[NormalizedUserName]    NVARCHAR(256) NULL,
	[Email]                 NVARCHAR(256) NULL,
	[NormalizedEmail]       NVARCHAR(256) NULL,
	[EmailConfirmed]        BIT NOT NULL CONSTRAINT [DF_AspNetUsers_EmailConfirmed] DEFAULT (0),
	[PasswordHash]          NVARCHAR(MAX) NULL,
	[SecurityStamp]         NVARCHAR(MAX) NULL,
	[ConcurrencyStamp]      NVARCHAR(MAX) NULL,
	[PhoneNumber]           NVARCHAR(MAX) NULL,
	[PhoneNumberConfirmed]  BIT NOT NULL CONSTRAINT [DF_AspNetUsers_PhoneNumberConfirmed] DEFAULT (0),
	[TwoFactorEnabled]      BIT NOT NULL CONSTRAINT [DF_AspNetUsers_TwoFactorEnabled] DEFAULT (0),
	[LockoutEnd]            DATETIMEOFFSET(7) NULL,
	[LockoutEnabled]        BIT NOT NULL CONSTRAINT [DF_AspNetUsers_LockoutEnabled] DEFAULT (0),
	[AccessFailedCount]     INT NOT NULL CONSTRAINT [DF_AspNetUsers_AccessFailedCount] DEFAULT (0),
	CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [dbo].[AspNetRoleClaims]
(
	[Id]         INT IDENTITY(1,1) NOT NULL,
	[RoleId]     INT NOT NULL,
	[ClaimType]  NVARCHAR(MAX) NULL,
	[ClaimValue] NVARCHAR(MAX) NULL,
	CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
	CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId]
		FOREIGN KEY ([RoleId]) REFERENCES [dbo].[AspNetRoles]([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [dbo].[AspNetUserClaims]
(
	[Id]         INT IDENTITY(1,1) NOT NULL,
	[UserId]     INT NOT NULL,
	[ClaimType]  NVARCHAR(MAX) NULL,
	[ClaimValue] NVARCHAR(MAX) NULL,
	CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
	CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId]
		FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [dbo].[AspNetUserLogins]
(
	[LoginProvider]       NVARCHAR(450) NOT NULL,
	[ProviderKey]         NVARCHAR(450) NOT NULL,
	[ProviderDisplayName] NVARCHAR(MAX) NULL,
	[UserId]              INT NOT NULL,
	CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
	CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId]
		FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [dbo].[AspNetUserRoles]
(
	[UserId] INT NOT NULL,
	[RoleId] INT NOT NULL,
	CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
	CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId]
		FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE,
	CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId]
		FOREIGN KEY ([RoleId]) REFERENCES [dbo].[AspNetRoles]([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [dbo].[AspNetUserTokens]
(
	[UserId]        INT NOT NULL,
	[LoginProvider] NVARCHAR(450) NOT NULL,
	[Name]          NVARCHAR(450) NOT NULL,
	[Value]         NVARCHAR(MAX) NULL,
	CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
	CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId]
		FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [dbo].[RefreshTokens]
(
	[Id]        INT IDENTITY(1,1) NOT NULL,
	[UserId]    INT NOT NULL,
	[Token]     NVARCHAR(200) NOT NULL,
	[ExpiresAt] DATETIME2 NOT NULL,
	[CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_RefreshTokens_CreatedAt] DEFAULT (SYSUTCDATETIME()),
	[IsRevoked] BIT NOT NULL CONSTRAINT [DF_RefreshTokens_IsRevoked] DEFAULT (0),
	CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
	CONSTRAINT [FK_RefreshTokens_AspNetUsers_UserId]
		FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [dbo].[AccessTokenAudits]
(
	[Id]        INT IDENTITY(1,1) NOT NULL,
	[UserId]    INT NOT NULL,
	[Token]     NVARCHAR(850) NOT NULL,
	[ExpiresAt] DATETIME2 NOT NULL,
	[IssuedAt]  DATETIME2 NOT NULL CONSTRAINT [DF_AccessTokenAudits_IssuedAt] DEFAULT (SYSUTCDATETIME()),
	[IsRevoked] BIT NOT NULL CONSTRAINT [DF_AccessTokenAudits_IsRevoked] DEFAULT (0),
	[RevokedAt] DATETIME2 NULL,
	CONSTRAINT [PK_AccessTokenAudits] PRIMARY KEY ([Id]),
	CONSTRAINT [FK_AccessTokenAudits_AspNetUsers_UserId]
		FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX [RoleNameIndex] ON [dbo].[AspNetRoles]([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO
CREATE UNIQUE INDEX [UserNameIndex] ON [dbo].[AspNetUsers]([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO
CREATE INDEX [EmailIndex] ON [dbo].[AspNetUsers]([NormalizedEmail]);
GO
CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [dbo].[AspNetRoleClaims]([RoleId]);
GO
CREATE INDEX [IX_AspNetUserClaims_UserId] ON [dbo].[AspNetUserClaims]([UserId]);
GO
CREATE INDEX [IX_AspNetUserLogins_UserId] ON [dbo].[AspNetUserLogins]([UserId]);
GO
CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [dbo].[AspNetUserRoles]([RoleId]);
GO
CREATE UNIQUE INDEX [IX_RefreshTokens_Token] ON [dbo].[RefreshTokens]([Token]);
GO
CREATE INDEX [IX_RefreshTokens_UserId] ON [dbo].[RefreshTokens]([UserId]);
GO
CREATE UNIQUE INDEX [IX_AccessTokenAudits_Token] ON [dbo].[AccessTokenAudits]([Token]);
GO
CREATE INDEX [IX_AccessTokenAudits_UserId] ON [dbo].[AccessTokenAudits]([UserId]);
GO

INSERT INTO [dbo].[AspNetRoles] ([Name], [NormalizedName], [ConcurrencyStamp])
VALUES (N'Admin', N'ADMIN', CONVERT(NVARCHAR(36), NEWID())),
	   (N'User',  N'USER',  CONVERT(NVARCHAR(36), NEWID()));
GO

USE [UserDb];
GO

CREATE TABLE [dbo].[UserProfiles]
(
	[Id]         INT IDENTITY(1,1) NOT NULL,
	[AuthUserId] INT NOT NULL,
	[FullName]   NVARCHAR(200) NOT NULL,
	[Email]      NVARCHAR(256) NOT NULL,
	[Role]       NVARCHAR(50) NOT NULL CONSTRAINT [DF_UserProfiles_Role] DEFAULT (N'User'),
	[CreatedAt]  DATETIME2 NOT NULL CONSTRAINT [DF_UserProfiles_CreatedAt] DEFAULT (SYSUTCDATETIME()),
	[UpdatedAt]  DATETIME2 NOT NULL CONSTRAINT [DF_UserProfiles_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
	CONSTRAINT [PK_UserProfiles] PRIMARY KEY ([Id])
);
GO

CREATE UNIQUE INDEX [IX_UserProfiles_AuthUserId] ON [dbo].[UserProfiles]([AuthUserId]);
GO
CREATE UNIQUE INDEX [IX_UserProfiles_Email] ON [dbo].[UserProfiles]([Email]);
GO
```

## 7. Configuration

### AuthService `appsettings.json`
- `ConnectionStrings:DefaultConnection` -> `AuthDb`
- `JwtSettings` -> issuer/audience/secret/expiry
- `BootstrapAdmin` -> seeded admin credentials

### UserService `appsettings.json`
- `ConnectionStrings:DefaultConnection` -> `UserDb`
- `AuthService:BaseUrl` -> AuthService URL for registration orchestration
- `JwtSettings` -> must match AuthService for token validation

## 8. Authorization Matrix
- `POST /api/users/register` -> Anonymous
- `GET /api/users/me`, `PUT /api/users/me` -> Authenticated user
- `GET /api/users`, `GET /api/users/{id}`, `DELETE /api/users/{id}`, `PATCH /api/users/{id}/role` -> Admin only
- `POST /api/auth/login`, `POST /api/auth/refresh-token` -> Anonymous
- `POST /api/auth/logout`, `POST /api/auth/register-admin` -> Authenticated (admin for register-admin)

## 9. Notes
- There are no stored procedures in this solution currently.
- System uses SQL tables + EF Core directly.
- Access token auditing currently stores the token string in `AuthDb.dbo.AccessTokenAudits`.
