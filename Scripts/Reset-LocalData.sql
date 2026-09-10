/*
DEV-ONLY LOCAL RESET SCRIPT
Target: SQL Server LocalDB / RentalAppDb
Purpose: Clear application data while preserving schema and EF migration history.

Important:
- Review before running.
- This script preserves __EFMigrationsHistory so EF still recognizes the schema.
- By default it also preserves Users so you do not lock yourself out locally.
- Set @DeleteUsers = 1 if you want a completely blank login/user state.
- This script uses DELETE + DBCC CHECKIDENT because TRUNCATE is blocked by foreign keys.
*/

USE [RentalAppDb];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

DECLARE @DeleteUsers bit = 0;

BEGIN TRANSACTION;

BEGIN TRY
	-- Child / dependent tables first
	DELETE FROM dbo.InvoiceItems;
	DELETE FROM dbo.UtilityCustomerCredits;
	DELETE FROM dbo.UtilityBillPayments;
	DELETE FROM dbo.Payments;
	DELETE FROM dbo.Invoices;
	DELETE FROM dbo.UtilityBills;
	DELETE FROM dbo.UtilityAuditLogs;
	DELETE FROM dbo.Expenses;
	DELETE FROM dbo.UtilityCustomers;
	DELETE FROM dbo.Leases;
	DELETE FROM dbo.Rooms;
	DELETE FROM dbo.Tenants;
	DELETE FROM dbo.Units;
	DELETE FROM dbo.Properties;

	-- Reference / setup tables
	DELETE FROM dbo.ExpenseCategories;
	DELETE FROM dbo.UtilityTypes;

	-- Optional local auth reset
	IF (@DeleteUsers = 1)
	BEGIN
		DELETE FROM dbo.Users;
	END

	-- Identity reseed to start fresh
	DBCC CHECKIDENT ('dbo.InvoiceItems', RESEED, 0) WITH NO_INFOMSGS;
	DBCC CHECKIDENT ('dbo.UtilityCustomerCredits', RESEED, 0) WITH NO_INFOMSGS;
	DBCC CHECKIDENT ('dbo.UtilityBillPayments', RESEED, 0) WITH NO_INFOMSGS;
	DBCC CHECKIDENT ('dbo.Payments', RESEED, 0) WITH NO_INFOMSGS;
	DBCC CHECKIDENT ('dbo.Invoices', RESEED, 0) WITH NO_INFOMSGS;
	DBCC CHECKIDENT ('dbo.UtilityBills', RESEED, 0) WITH NO_INFOMSGS;
	DBCC CHECKIDENT ('dbo.UtilityAuditLogs', RESEED, 0) WITH NO_INFOMSGS;
	DBCC CHECKIDENT ('dbo.Expenses', RESEED, 0) WITH NO_INFOMSGS;
	DBCC CHECKIDENT ('dbo.UtilityCustomers', RESEED, 0) WITH NO_INFOMSGS;
	DBCC CHECKIDENT ('dbo.Leases', RESEED, 0) WITH NO_INFOMSGS;
	DBCC CHECKIDENT ('dbo.Rooms', RESEED, 0) WITH NO_INFOMSGS;
	DBCC CHECKIDENT ('dbo.Tenants', RESEED, 0) WITH NO_INFOMSGS;
	DBCC CHECKIDENT ('dbo.Units', RESEED, 0) WITH NO_INFOMSGS;
	DBCC CHECKIDENT ('dbo.Properties', RESEED, 0) WITH NO_INFOMSGS;
	DBCC CHECKIDENT ('dbo.ExpenseCategories', RESEED, 0) WITH NO_INFOMSGS;
	DBCC CHECKIDENT ('dbo.UtilityTypes', RESEED, 0) WITH NO_INFOMSGS;

	IF (@DeleteUsers = 1)
	BEGIN
		DBCC CHECKIDENT ('dbo.Users', RESEED, 0) WITH NO_INFOMSGS;
	END

	COMMIT TRANSACTION;
END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0
		ROLLBACK TRANSACTION;

	THROW;
END CATCH;
GO

-- Optional verification
SELECT 'Properties' AS TableName, COUNT(*) AS RowCount FROM dbo.Properties
UNION ALL SELECT 'Units', COUNT(*) FROM dbo.Units
UNION ALL SELECT 'Rooms', COUNT(*) FROM dbo.Rooms
UNION ALL SELECT 'Tenants', COUNT(*) FROM dbo.Tenants
UNION ALL SELECT 'Leases', COUNT(*) FROM dbo.Leases
UNION ALL SELECT 'Payments', COUNT(*) FROM dbo.Payments
UNION ALL SELECT 'Invoices', COUNT(*) FROM dbo.Invoices
UNION ALL SELECT 'InvoiceItems', COUNT(*) FROM dbo.InvoiceItems
UNION ALL SELECT 'Expenses', COUNT(*) FROM dbo.Expenses
UNION ALL SELECT 'ExpenseCategories', COUNT(*) FROM dbo.ExpenseCategories
UNION ALL SELECT 'UtilityTypes', COUNT(*) FROM dbo.UtilityTypes
UNION ALL SELECT 'UtilityCustomers', COUNT(*) FROM dbo.UtilityCustomers
UNION ALL SELECT 'UtilityBills', COUNT(*) FROM dbo.UtilityBills
UNION ALL SELECT 'UtilityBillPayments', COUNT(*) FROM dbo.UtilityBillPayments
UNION ALL SELECT 'UtilityCustomerCredits', COUNT(*) FROM dbo.UtilityCustomerCredits
UNION ALL SELECT 'UtilityAuditLogs', COUNT(*) FROM dbo.UtilityAuditLogs
UNION ALL SELECT 'Users', COUNT(*) FROM dbo.Users
UNION ALL SELECT '__EFMigrationsHistory', COUNT(*) FROM dbo.__EFMigrationsHistory;
GO
