# RentalApp Production Checklist

## Hosting
- [ ] MonsterASP website created
- [ ] .NET 10 runtime confirmed
- [ ] Production hostname confirmed
- [ ] HTTPS confirmed
- [ ] Writable persistent application-data directory confirmed

## Configuration
- [ ] ASPNETCORE_ENVIRONMENT configured
- [ ] Production SQL Server connection configured
- [ ] JWT issuer configured
- [ ] JWT audience configured
- [ ] Strong JWT key configured
- [ ] Application data paths configured

## GitHub
- [ ] Production secrets configured
- [ ] Deployment workflow enabled
- [ ] Database excluded from repository
- [ ] Database excluded from publish
- [ ] Build succeeds
- [ ] Tests succeed
- [ ] Deployment succeeds

## Database
- [ ] Production database created
- [ ] Latest schema applied
- [ ] Existing data verified
- [ ] Hosting or SQL Server backup process verified
- [ ] Hosting or SQL Server restore procedure verified
- [ ] Redeployment does not overwrite database

## Security
- [ ] Authentication verified
- [ ] Authorization verified
- [ ] JWT-protected APIs verified
- [ ] CSRF protection verified
- [ ] HTTPS verified
- [ ] Security headers verified
- [ ] Production Swagger disabled
- [ ] Sensitive files inaccessible
- [ ] Self-service password reset disabled or securely redesigned
- [ ] Secrets not committed

## Functional Smoke Test
- [ ] Login
- [ ] Dashboard
- [ ] Tenant
- [ ] Apartment/unit
- [ ] Room
- [ ] Payment
- [ ] Expense
- [ ] Invoice
- [ ] Utility
- [ ] Reports
- [ ] Logout
