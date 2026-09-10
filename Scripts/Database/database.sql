IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE TABLE [ExpenseCategories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_ExpenseCategories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE TABLE [Properties] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Address] nvarchar(500) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Properties] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE TABLE [Tenants] (
        [Id] int NOT NULL IDENTITY,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [ContactNumber] nvarchar(30) NULL,
        [Email] nvarchar(256) NULL,
        [Address] nvarchar(500) NULL,
        [UnitId] int NULL,
        [UnitNumber] nvarchar(50) NULL,
        [RoomNumber] nvarchar(50) NULL,
        [DateOfBirth] datetime2 NULL,
        [Notes] nvarchar(2000) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Tenants] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [Username] nvarchar(100) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE TABLE [UtilityAuditLogs] (
        [Id] int NOT NULL IDENTITY,
        [EntityName] nvarchar(100) NOT NULL,
        [EntityId] nvarchar(100) NOT NULL,
        [Action] int NOT NULL,
        [OldValuesJson] nvarchar(max) NULL,
        [NewValuesJson] nvarchar(max) NULL,
        [PerformedBy] nvarchar(100) NULL,
        [PerformedAt] datetime2 NOT NULL,
        [CorrelationId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_UtilityAuditLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE TABLE [UtilityTypes] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Unit] nvarchar(20) NOT NULL,
        [DefaultRate] decimal(18,4) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_UtilityTypes] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_UtilityTypes_DefaultRate_NonNegative] CHECK (DefaultRate IS NULL OR DefaultRate >= 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE TABLE [Units] (
        [Id] int NOT NULL IDENTITY,
        [PropertyId] int NOT NULL,
        [UnitNumber] nvarchar(50) NOT NULL,
        [MonthlyRent] decimal(18,2) NOT NULL,
        [Status] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Units] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Units_Properties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Properties] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE TABLE [Expenses] (
        [Id] int NOT NULL IDENTITY,
        [PropertyId] int NOT NULL,
        [UnitId] int NULL,
        [CategoryId] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [ExpenseDate] datetime2 NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Expenses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Expenses_ExpenseCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [ExpenseCategories] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Expenses_Properties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Properties] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Expenses_Units_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [Units] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE TABLE [Rooms] (
        [Id] int NOT NULL IDENTITY,
        [UnitId] int NOT NULL,
        [RoomNumber] nvarchar(50) NOT NULL,
        [MaxCapacity] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Rooms] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Rooms_Units_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [Units] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE TABLE [Leases] (
        [Id] int NOT NULL IDENTITY,
        [UnitId] int NOT NULL,
        [RoomId] int NOT NULL,
        [TenantId] int NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NULL,
        [MonthlyRent] decimal(18,2) NOT NULL,
        [SecurityDeposit] decimal(18,2) NOT NULL,
        [DueDayOfMonth] int NOT NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Leases] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Leases_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Leases_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Leases_Units_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [Units] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE TABLE [UtilityCustomers] (
        [Id] int NOT NULL IDENTITY,
        [RoomId] int NULL,
        [TenantId] int NULL,
        [UtilityCategoryId] int NULL,
        [Name] nvarchar(200) NOT NULL,
        [CustomerType] int NOT NULL,
        [DueDateRuleType] int NOT NULL,
        [UtilityStartDate] datetime2 NULL,
        [DueDayOfMonth] int NULL,
        [DueInDays] int NULL,
        [DefaultRate] decimal(18,4) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_UtilityCustomers] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_UtilityCustomers_DefaultRate_NonNegative] CHECK (DefaultRate IS NULL OR DefaultRate >= 0),
        CONSTRAINT [CK_UtilityCustomers_DueDay_Range] CHECK (DueDayOfMonth IS NULL OR (DueDayOfMonth >= 1 AND DueDayOfMonth <= 28)),
        CONSTRAINT [CK_UtilityCustomers_DueInDays_NonNegative] CHECK (DueInDays IS NULL OR DueInDays >= 0),
        CONSTRAINT [FK_UtilityCustomers_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UtilityCustomers_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UtilityCustomers_UtilityTypes_UtilityCategoryId] FOREIGN KEY ([UtilityCategoryId]) REFERENCES [UtilityTypes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE TABLE [Invoices] (
        [Id] int NOT NULL IDENTITY,
        [InvoiceNumber] nvarchar(50) NOT NULL,
        [TenantId] int NOT NULL,
        [LeaseId] int NOT NULL,
        [InvoiceDate] datetime2 NOT NULL,
        [DueDate] datetime2 NOT NULL,
        [Subtotal] decimal(18,2) NOT NULL,
        [Total] decimal(18,2) NOT NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Invoices_Leases_LeaseId] FOREIGN KEY ([LeaseId]) REFERENCES [Leases] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Invoices_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE TABLE [Payments] (
        [Id] int NOT NULL IDENTITY,
        [LeaseId] int NOT NULL,
        [TenantId] int NOT NULL,
        [UnitId] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [PaymentDate] datetime2 NOT NULL,
        [PaymentType] int NOT NULL,
        [PaymentMethod] int NOT NULL,
        [ReferenceNumber] nvarchar(100) NULL,
        [Notes] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payments_Leases_LeaseId] FOREIGN KEY ([LeaseId]) REFERENCES [Leases] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Payments_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Payments_Units_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [Units] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE TABLE [UtilityBills] (
        [Id] int NOT NULL IDENTITY,
        [UtilityCustomerId] int NOT NULL,
        [UtilityTypeId] int NOT NULL,
        [BillingPeriod] nvarchar(7) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [DueDate] datetime2 NULL,
        [Status] int NOT NULL,
        [Version] varbinary(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_UtilityBills] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_UtilityBills_Amount_NonNegative] CHECK (Amount >= 0),
        CONSTRAINT [CK_UtilityBills_BillingPeriod_Length] CHECK (LEN(BillingPeriod) = 7),
        CONSTRAINT [FK_UtilityBills_UtilityCustomers_UtilityCustomerId] FOREIGN KEY ([UtilityCustomerId]) REFERENCES [UtilityCustomers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UtilityBills_UtilityTypes_UtilityTypeId] FOREIGN KEY ([UtilityTypeId]) REFERENCES [UtilityTypes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE TABLE [InvoiceItems] (
        [Id] int NOT NULL IDENTITY,
        [InvoiceId] int NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_InvoiceItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InvoiceItems_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE TABLE [UtilityBillPayments] (
        [Id] int NOT NULL IDENTITY,
        [UtilityBillId] int NOT NULL,
        [PaymentMethodId] int NULL,
        [PaymentDate] datetime2 NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [ReferenceNumber] nvarchar(100) NULL,
        [Notes] nvarchar(1000) NULL,
        [IsVoided] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_UtilityBillPayments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_UtilityBillPayments_Amount_Positive] CHECK (Amount > 0),
        CONSTRAINT [FK_UtilityBillPayments_UtilityBills_UtilityBillId] FOREIGN KEY ([UtilityBillId]) REFERENCES [UtilityBills] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE TABLE [UtilityCustomerCredits] (
        [Id] int NOT NULL IDENTITY,
        [UtilityCustomerId] int NOT NULL,
        [UtilityTypeId] int NOT NULL,
        [SourcePaymentId] int NULL,
        [Amount] decimal(18,2) NOT NULL,
        [BalanceAfter] decimal(18,2) NOT NULL,
        [TransactionType] int NOT NULL,
        [OccurredAt] datetime2 NOT NULL,
        [Notes] nvarchar(1000) NULL,
        CONSTRAINT [PK_UtilityCustomerCredits] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UtilityCustomerCredits_UtilityBillPayments_SourcePaymentId] FOREIGN KEY ([SourcePaymentId]) REFERENCES [UtilityBillPayments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UtilityCustomerCredits_UtilityCustomers_UtilityCustomerId] FOREIGN KEY ([UtilityCustomerId]) REFERENCES [UtilityCustomers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UtilityCustomerCredits_UtilityTypes_UtilityTypeId] FOREIGN KEY ([UtilityTypeId]) REFERENCES [UtilityTypes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ExpenseCategories_Name] ON [ExpenseCategories] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_Expenses_CategoryId] ON [Expenses] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_Expenses_ExpenseDate] ON [Expenses] ([ExpenseDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_Expenses_PropertyId] ON [Expenses] ([PropertyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_Expenses_UnitId] ON [Expenses] ([UnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_InvoiceItems_InvoiceId] ON [InvoiceItems] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_Invoices_DueDate] ON [Invoices] ([DueDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_Invoices_InvoiceDate] ON [Invoices] ([InvoiceDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Invoices_InvoiceNumber] ON [Invoices] ([InvoiceNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_Invoices_LeaseId] ON [Invoices] ([LeaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_Invoices_Status] ON [Invoices] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_Invoices_TenantId] ON [Invoices] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_Leases_RoomId_Status] ON [Leases] ([RoomId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_Leases_TenantId_Status] ON [Leases] ([TenantId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_Leases_UnitId_Status] ON [Leases] ([UnitId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_Payments_LeaseId] ON [Payments] ([LeaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_Payments_PaymentDate] ON [Payments] ([PaymentDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_Payments_TenantId] ON [Payments] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_Payments_UnitId] ON [Payments] ([UnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_Properties_Name] ON [Properties] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Rooms_UnitId_RoomNumber] ON [Rooms] ([UnitId], [RoomNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_Tenants_FirstName_LastName] ON [Tenants] ([FirstName], [LastName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Units_PropertyId_UnitNumber] ON [Units] ([PropertyId], [UnitNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_Units_Status] ON [Units] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_UtilityAuditLogs_CorrelationId] ON [UtilityAuditLogs] ([CorrelationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_UtilityAuditLogs_EntityName_EntityId] ON [UtilityAuditLogs] ([EntityName], [EntityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_UtilityBillPayments_PaymentDate] ON [UtilityBillPayments] ([PaymentDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_UtilityBillPayments_UtilityBillId] ON [UtilityBillPayments] ([UtilityBillId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_UtilityBills_DueDate] ON [UtilityBills] ([DueDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_UtilityBills_Status] ON [UtilityBills] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UtilityBills_UtilityCustomerId_UtilityTypeId_BillingPeriod] ON [UtilityBills] ([UtilityCustomerId], [UtilityTypeId], [BillingPeriod]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_UtilityBills_UtilityTypeId] ON [UtilityBills] ([UtilityTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_UtilityCustomerCredits_SourcePaymentId] ON [UtilityCustomerCredits] ([SourcePaymentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_UtilityCustomerCredits_UtilityCustomerId_UtilityTypeId_OccurredAt] ON [UtilityCustomerCredits] ([UtilityCustomerId], [UtilityTypeId], [OccurredAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_UtilityCustomerCredits_UtilityTypeId] ON [UtilityCustomerCredits] ([UtilityTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_UtilityCustomers_CustomerType] ON [UtilityCustomers] ([CustomerType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_UtilityCustomers_Name] ON [UtilityCustomers] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_UtilityCustomers_RoomId] ON [UtilityCustomers] ([RoomId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_UtilityCustomers_TenantId] ON [UtilityCustomers] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE INDEX [IX_UtilityCustomers_UtilityCategoryId] ON [UtilityCustomers] ([UtilityCategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UtilityTypes_Code] ON [UtilityTypes] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909094959_InitialSqlServerBaseline'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260909094959_InitialSqlServerBaseline', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909095027_InitialMigration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260909095027_InitialMigration', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909101101_AddMoveInDateToTenant'
)
BEGIN
    ALTER TABLE [Tenants] ADD [MoveInDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909101101_AddMoveInDateToTenant'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260909101101_AddMoveInDateToTenant', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909110532_AddDueDateToPayments'
)
BEGIN
    ALTER TABLE [Payments] ADD [DueDate] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909110532_AddDueDateToPayments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260909110532_AddDueDateToPayments', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910023112_AddUnitMaxCapacity'
)
BEGIN
    ALTER TABLE [Units] ADD [MaxCapacity] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910023112_AddUnitMaxCapacity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260910023112_AddUnitMaxCapacity', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910030000_SetUnitMaxCapacityDefaultToZero'
)
BEGIN

    DECLARE @constraintName nvarchar(128);

    SELECT @constraintName = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    INNER JOIN sys.tables t ON t.object_id = c.object_id
    WHERE t.name = 'Units'
      AND SCHEMA_NAME(t.schema_id) = 'dbo'
      AND c.name = 'MaxCapacity';

    IF @constraintName IS NOT NULL
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Units] DROP CONSTRAINT [' + @constraintName + ']');
    END;

    ALTER TABLE [dbo].[Units] ADD DEFAULT (0) FOR [MaxCapacity];

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910030000_SetUnitMaxCapacityDefaultToZero'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260910030000_SetUnitMaxCapacityDefaultToZero', N'10.0.11');
END;

COMMIT;
GO

