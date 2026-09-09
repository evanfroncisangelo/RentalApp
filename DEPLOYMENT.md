# RentalApp Deployment Guide

## 1. Local Development
- Required SDK/runtime: **.NET 10**
- Run locally:
  - `dotnet restore RentalApp.slnx`
  - `dotnet build RentalApp.slnx -c Release`
  - `dotnet run --project RentalApp.csproj`
- Local environment should use `ASPNETCORE_ENVIRONMENT=Development`.

## 2. Required .NET Version
- Application targets `net10.0`.
- MonsterASP deployment target must support .NET 10 runtime.

## 3. Database Setup
- SQLite is the primary database.
- Configure with `ConnectionStrings__DefaultConnection`.
- Configure `ApplicationData__DatabasePath` to the same file used by `DefaultConnection` (`Data Source=...`).
- Database file must be outside `wwwroot`.

## 4. EF Core Migrations
- Historical migrations are kept in `Migrations/` and should not be edited.
- Local migration apply command:
  - `dotnet ef database update --project RentalApp.csproj`

## 5. Production SQLite Setup
- Set MonsterASP writable persistent application data root.
- Store DB, backups, and uploads in that persistent location.
- Do not place SQLite files in repository, publish output, or static web folders.

## 6. MonsterASP Setup
- Create website/app with .NET 10 support.
- Configure environment variables in hosting panel.
- Confirm writable persistent path for application data.

## 7. HTTPS
- Production must run over HTTPS.
- Keep HSTS enabled in production.

## 8. Environment Variables (Production)
Set in MonsterASP (example names):
- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__DefaultConnection`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__Key`
- `Jwt__ExpiresMinutes`
- `ApplicationData__RootPath`
- `ApplicationData__DatabasePath`
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
Pipeline order:
1. Checkout
2. Restore
3. Build (Release)
4. Test (Release)
5. Publish (Release)
6. Remove development/private artifacts
7. Deploy via WebDeploy

Deployment is blocked if restore/build/test/publish fails.

## 11. Database Backup
- Use authenticated endpoint: `POST /api/system/backups/create`
- Backups are generated with SQLite backup API.
- Backup files are timestamped and written to `ApplicationData__BackupsPath`.
- Retention uses `ApplicationData__BackupRetentionDays`.

## 12. Database Restore
- Use authenticated endpoint: `POST /api/system/backups/restore`
- Request must include:
  - managed backup file name
  - confirmation phrase `RESTORE`
- Restore creates a pre-restore safety backup automatically before applying restore.
- Restore is always operator-triggered (never automatic during deployment).

## 13. Production Migration Procedure
1. Create a DB backup.
2. Validate migration in staging/local copy.
3. Run `dotnet ef database update` in controlled maintenance window.
4. Run smoke tests.
5. If failure: restore from pre-migration backup.

## 14. Troubleshooting
- Startup fails in production:
  - check required env variables and writable paths.
- DB write failures:
  - verify folder permissions and DB path consistency.
- Deployment failure:
  - verify WebDeploy endpoint/credentials and SSL trust requirement.

## 15. Redeployment Behavior
- CI publish excludes DB files (`*.db`, `*.sqlite*`, WAL/SHM) and operational folders.
- Deployment pushes app binaries/content only.
- Existing production DB remains on persistent storage and is not overwritten by redeploy.

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
14. Confirm DB files are not publicly downloadable.
15. Create a manual backup via API.
16. Recycle app and verify data remains.
17. Deploy again and confirm no data overwrite.

No secrets, credentials, or host-specific filesystem guesses should be committed to source control.
