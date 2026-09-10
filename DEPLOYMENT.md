# RentalApp Deployment Guide

## 1. Local Development
- Required SDK/runtime: **.NET 10**
- Run locally:
  - `dotnet restore RentalApp.slnx`
  - `dotnet build RentalApp.slnx -c Release`
  - `dotnet run --project RentalApp.csproj`
- Local environment should use `ASPNETCORE_ENVIRONMENT=Development`.
- Development is currently configured for SQL Server LocalDB through `appsettings.Development.json`.

## 2. Required .NET Version
- Application targets `net10.0`.
- MonsterASP deployment target must support .NET 10 runtime.

## 3. Database Setup
- SQL Server is the primary database provider.
- Configure the application with `ConnectionStrings__DefaultConnection`.
- Development currently uses LocalDB:
  - `Server=(localdb)\MSSQLLocalDB;Database=RentalAppDb;Trusted_Connection=True;MultipleActiveResultSets=True;Encrypt=False;TrustServerCertificate=True`
- Production must use a valid SQL Server connection string for the target SQL Server instance.
- The application uses EF Core SQL Server provider via `UseSqlServer(...)` in `Program.cs`.

## 4. EF Core Migrations
- Historical migrations are kept in `Migrations/` and should not be edited.
- Apply migrations with:
  - `dotnet ef database update --project RentalApp.csproj`
- Run migrations against the SQL Server instance referenced by `ConnectionStrings__DefaultConnection`.

## 5. Production SQL Server Setup
- Provision a SQL Server database that the deployed app can reach.
- Ensure the SQL login/user has permission to connect and perform normal application reads/writes.
- Configure a writable application data root for operational files such as uploads and backup file output.
- Do not store secrets or production connection strings in source control.

## 6. MonsterASP Setup
- Create website/app with .NET 10 support.
- Configure environment variables in hosting panel.
- Confirm writable persistent path for application data.
- Confirm the hosted app can reach the target SQL Server instance over the required network path.

## 7. HTTPS
- Production must run over HTTPS.
- Keep HSTS enabled in production.

## 7.1 Authentication Modes
- Razor Pages use secure cookie authentication for browser sessions.
- API controllers are intended for JWT Bearer authentication.
- Do not rely on browser cookies to call protected API endpoints from third-party clients.
- Self-service forgot-password is disabled outside development until a secure reset flow is implemented.

## 8. Environment Variables (Production)
Set in MonsterASP (example names):
- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__DefaultConnection`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__Key`
- `Jwt__ExpiresMinutes`
- `ApplicationData__RootPath`
- `ApplicationData__UploadsPath`
- `ApplicationData__BackupsPath`
- `ApplicationData__BackupRetentionDays`
- `AllowedHosts`

## 9. GitHub Secrets
Required repository secrets:
- `WEBSITE_NAME`
- `SERVER_COMPUTER_NAME`
- `SERVER_USERNAME`
- `SERVER_PASSWORD`
- Optional: `WEBDEPLOY_ALLOW_UNTRUSTED` (`true` only if host requires untrusted cert deployment)

## 10. GitHub Actions Deployment
Workflow: `.github/workflows/deploy.yml`

Production deployment triggers:
- Automatic on semantic version tag push matching `v*.*.*`.
- Manual via `workflow_dispatch` with required `release_tag` input.
- Tag format is validated with `^v\d+\.\d+\.\d+$` before deployment.

Pipeline order:
1. Resolve and validate deployment tag
2. Checkout exact tag
3. Restore
4. Build (Release)
5. Test (Release)
6. Publish (Release)
7. Remove development/private artifacts
8. Verify `publish/RentalApp.dll` exists
9. Deploy via WebDeploy

Deployment is blocked if restore/build/test/publish/verification fails.

Rollback strategy:
- Re-run the workflow manually with a previously known-good semantic tag.
- This redeploys the selected application version without changing Git history.

## 11. Database Backup
- Do not depend on application HTTP endpoints for production backup operations.
- Current backup/restore endpoints are development-only.
- Preferred production approach: use MonsterASP or SQL Server backup tooling outside the web app.
- If SQL Server native backups are used, the SQL Server service/account must be able to access the target backup path.
- Keep `.bak` files out of the web root and out of source control.

## 12. Database Restore
- Do not perform database restore through the production web application.
- Current restore endpoint is development-only.
- Preferred production approach: restore through hosting or SQL Server administrative tooling during a controlled maintenance window.
- Always create or confirm a valid backup before any restore action.

## 13. Production Migration Procedure
1. Create a SQL Server backup.
2. Validate migration in staging or a local SQL Server copy.
3. Run `dotnet ef database update` in a controlled maintenance window.
4. Run smoke tests.
5. If failure: restore from the pre-migration backup.

## 14. Troubleshooting
- Startup fails in production:
  - check required env variables and SQL Server connection string.
- Database connection failures:
  - verify SQL Server instance availability, credentials, firewall/network access, and TLS requirements.
- Backup or restore failures:
  - verify SQL Server has permission to read/write the configured backup directory.
- Deployment failure:
  - verify WebDeploy endpoint, credentials, and SSL trust requirement.

## 15. Redeployment Behavior
- CI publish excludes development settings, local database artifacts, and operational folders from the deploy package.
- Deployment pushes app binaries and content only.
- The production SQL Server database is external to the published site output and is not overwritten by redeploy.

## 16. Verify DB Survives Redeployment
1. Record current row count in a known table.
2. Run deployment workflow.
3. Re-open app and verify existing records remain.
4. Add a new record.
5. Recycle app and verify both old and new records still exist.

## 17. Post-Deployment Smoke Test
1. Open site over HTTPS.
2. Login with valid credentials.
3. Open Dashboard.
4. Confirm existing records are present.
5. Create one record (tenant/payment/expense).
6. Edit a record.
7. Create payment successfully.
8. Create expense successfully.
9. Create invoice successfully.
10. Perform one utility operation (payment/credit).
11. Logout and verify redirect to login.
12. Validate `/health` responds healthy.
13. Confirm Swagger UI is not exposed in production.
14. Confirm backup files are not publicly downloadable.
15. Create a manual backup via API.
16. Recycle app and verify data remains.
17. Deploy again and confirm no data overwrite.

No secrets, credentials, or host-specific filesystem guesses should be committed to source control.
