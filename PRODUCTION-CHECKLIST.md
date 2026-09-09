# RentalApp Production Checklist

## Hosting
- [ ] MonsterASP website created
- [ ] .NET 10 runtime confirmed
- [ ] Production hostname confirmed
- [ ] HTTPS confirmed
- [ ] Writable persistent application-data directory confirmed

## Configuration
- [ ] ASPNETCORE_ENVIRONMENT configured
- [ ] Production SQLite connection configured
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
- [ ] Backup tested
- [ ] Restore procedure tested
- [ ] Redeployment does not overwrite database

## Security
- [ ] Authentication verified
- [ ] Authorization verified
- [ ] CSRF protection verified
- [ ] HTTPS verified
- [ ] Security headers verified
- [ ] Production Swagger disabled
- [ ] Sensitive files inaccessible
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
