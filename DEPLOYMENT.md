# RentalApp Deployment Guide (Development + Production)

## 1) Environments
Only two environments are supported:
- Development
- Production

Set environment name:
- Local: `ASPNETCORE_ENVIRONMENT=Development`
- MonsterASP: `ASPNETCORE_ENVIRONMENT=Production`

---

## 2) Configuration Files
- `appsettings.json` -> safe shared defaults only
- `appsettings.Development.json` -> local non-secret defaults
- `appsettings.Production.json` -> structure only, values from server env

Production secrets must not be committed to source control.

---

## 3) Required Production Environment Variables
Set these in MonsterASP hosting configuration:
- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__DefaultConnection`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__Key`
- `ApplicationData__RootPath`
- `ApplicationData__DatabasePath`
- `ApplicationData__UploadsPath`
- `ApplicationData__BackupsPath`

Notes:
- Keep SQLite outside `wwwroot`.
- Example connection: `Data Source=<absolute-or-host-resolved-path-to-db-file>`

---

## 4) SQLite Location and Safety
- SQLite is configured via `ConnectionStrings:DefaultConnection`.
- Application validates required Production config at startup.
- Application creates required data directories at startup.
- Startup fails safely if Production data directory is not writable.
- Data paths must not resolve inside `wwwroot`.

---

## 5) EF Core Migrations
- Migrations are stored in source control.
- Do **not** run destructive DB reset commands in Production.
- Apply migrations with controlled operator action:
  - `dotnet ef database update`

If migration fails:
1. Stop and investigate.
2. Take/verify backup.
3. Retry with corrected migration path.

---

## 6) Backup Strategy (SQLite)
Recommended baseline:
- Daily backup (manual or scheduled by host capability)
- Keep last 7 backups
- Store backups outside `wwwroot`

Minimum manual backup procedure:
1. Quiesce writes (maintenance window).
2. Copy DB and related SQLite files (`.db`, `.db-wal`, `.db-shm`) if present.
3. Store backup in secure backup directory.
4. Verify backup by opening app against backup copy in a safe environment.

---

## 7) Restore Procedure
1. Stop/disable application writes.
2. Copy current DB to a timestamped safety backup.
3. Replace DB with verified backup set.
4. Verify file permissions.
5. Restart/recycle app.
6. Run migration/schema check.
7. Validate critical app flows (auth, CRUD, payments, utilities).

Never overwrite the only copy of Production DB.

---

## 8) GitHub Actions CI/CD
Workflow file:
- `.github/workflows/deploy.yml`

Pipeline order:
1. Checkout
2. Setup .NET 10
3. Restore
4. Build (Release)
5. Test (Release)
6. Publish (Release)
7. Remove non-deploy artifacts
8. Deploy via Web Deploy (IIS)

Deployment executes only if prior stages succeed.

---

## 9) Required GitHub Secrets
Configure repository secrets (names only):
- `WEBSITE_NAME`
- `SERVER_COMPUTER_NAME`
- `SERVER_USERNAME`
- `SERVER_PASSWORD`

Do not store these values in YAML or source code.

---

## 10) Security Notes
- Swagger enabled only in Development.
- Production uses exception handler + HSTS.
- Auth cookie configured for `HttpOnly`, `Secure`, `SameSite=Lax`.
- Baseline security headers are enabled.
- Health endpoint: `/health` (no sensitive payload).

---

## 11) Rollback
If deployment introduces issues:
1. Re-deploy last known good artifact.
2. Restore DB from verified backup if data issue exists.
3. Validate health endpoint and critical transactions.
4. Review logs without exposing secrets.

---

## 12) Troubleshooting
- Startup fails in Production with configuration error:
  - Check required env variables are present.
- DB write failures:
  - Verify configured directory exists and is writable by app pool identity.
- Deployment failure:
  - Verify Web Deploy endpoint, credentials, and secret names.

---

## 13) Security Checklist (Quick)
- No production secrets in source
- SQLite outside `wwwroot`
- DB files not committed to Git
- HTTPS working on Production site
- HSTS enabled in Production
- Unauthorized requests blocked for protected endpoints
- Backups available and restore tested
