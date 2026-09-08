using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalApp.Migrations
{
    /// <inheritdoc />
    public partial class UtilitiesV2Foundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UtilityAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntityName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Action = table.Column<int>(type: "INTEGER", nullable: false),
                    OldValuesJson = table.Column<string>(type: "TEXT", nullable: true),
                    NewValuesJson = table.Column<string>(type: "TEXT", nullable: true),
                    PerformedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PerformedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilityAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UtilityCustomers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoomId = table.Column<int>(type: "INTEGER", nullable: true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CustomerType = table.Column<int>(type: "INTEGER", nullable: false),
                    DueDateRuleType = table.Column<int>(type: "INTEGER", nullable: false),
                    DueDayOfMonth = table.Column<int>(type: "INTEGER", nullable: true),
                    DueInDays = table.Column<int>(type: "INTEGER", nullable: true),
                    DefaultRate = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilityCustomers", x => x.Id);
                    table.CheckConstraint("CK_UtilityCustomers_DefaultRate_NonNegative", "DefaultRate IS NULL OR DefaultRate >= 0");
                    table.CheckConstraint("CK_UtilityCustomers_DueDay_Range", "DueDayOfMonth IS NULL OR (DueDayOfMonth >= 1 AND DueDayOfMonth <= 28)");
                    table.CheckConstraint("CK_UtilityCustomers_DueInDays_NonNegative", "DueInDays IS NULL OR DueInDays >= 0");
                    table.ForeignKey(
                        name: "FK_UtilityCustomers_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UtilityCustomers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UtilityTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DefaultRate = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilityTypes", x => x.Id);
                    table.CheckConstraint("CK_UtilityTypes_DefaultRate_NonNegative", "DefaultRate IS NULL OR DefaultRate >= 0");
                });

            migrationBuilder.CreateTable(
                name: "UtilityCustomerRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UtilityCustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    UtilityTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    BillingPeriod = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IsLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilityCustomerRates", x => x.Id);
                    table.CheckConstraint("CK_UtilityCustomerRates_BillingPeriod_Length", "length(BillingPeriod) = 7");
                    table.CheckConstraint("CK_UtilityCustomerRates_Rate_NonNegative", "Rate >= 0");
                    table.ForeignKey(
                        name: "FK_UtilityCustomerRates_UtilityCustomers_UtilityCustomerId",
                        column: x => x.UtilityCustomerId,
                        principalTable: "UtilityCustomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UtilityCustomerRates_UtilityTypes_UtilityTypeId",
                        column: x => x.UtilityTypeId,
                        principalTable: "UtilityTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UtilityBillPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UtilityBillId = table.Column<int>(type: "INTEGER", nullable: false),
                    PaymentMethodId = table.Column<int>(type: "INTEGER", nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsVoided = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilityBillPayments", x => x.Id);
                    table.CheckConstraint("CK_UtilityBillPayments_Amount_Positive", "Amount > 0");
                });

            migrationBuilder.CreateTable(
                name: "UtilityCustomerCredits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UtilityCustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    UtilityTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    SourcePaymentId = table.Column<int>(type: "INTEGER", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransactionType = table.Column<int>(type: "INTEGER", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilityCustomerCredits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UtilityCustomerCredits_UtilityBillPayments_SourcePaymentId",
                        column: x => x.SourcePaymentId,
                        principalTable: "UtilityBillPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UtilityCustomerCredits_UtilityCustomers_UtilityCustomerId",
                        column: x => x.UtilityCustomerId,
                        principalTable: "UtilityCustomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UtilityCustomerCredits_UtilityTypes_UtilityTypeId",
                        column: x => x.UtilityTypeId,
                        principalTable: "UtilityTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UtilityBillResponsibilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UtilityBillId = table.Column<int>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: true),
                    FromDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ToDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DaysCovered = table.Column<int>(type: "INTEGER", nullable: false),
                    PercentageShare = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    AmountShare = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilityBillResponsibilities", x => x.Id);
                    table.CheckConstraint("CK_UtilityBillResponsibilities_AmountShare_NonNegative", "AmountShare >= 0");
                    table.CheckConstraint("CK_UtilityBillResponsibilities_DaysCovered_NonNegative", "DaysCovered >= 0");
                    table.CheckConstraint("CK_UtilityBillResponsibilities_Percentage_NonNegative", "PercentageShare >= 0");
                    table.ForeignKey(
                        name: "FK_UtilityBillResponsibilities_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UtilityBills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UtilityCustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    UtilityTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    BillingPeriod = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    PreviousReading = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CurrentReading = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Consumption = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedFromReadingId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsRecalculated = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecalculationBatchId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilityBills", x => x.Id);
                    table.CheckConstraint("CK_UtilityBills_Amount_NonNegative", "Amount >= 0");
                    table.CheckConstraint("CK_UtilityBills_BillingPeriod_Length", "length(BillingPeriod) = 7");
                    table.CheckConstraint("CK_UtilityBills_Consumption_NonNegative", "Consumption >= 0");
                    table.CheckConstraint("CK_UtilityBills_Rate_NonNegative", "Rate >= 0");
                    table.ForeignKey(
                        name: "FK_UtilityBills_UtilityCustomers_UtilityCustomerId",
                        column: x => x.UtilityCustomerId,
                        principalTable: "UtilityCustomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UtilityBills_UtilityTypes_UtilityTypeId",
                        column: x => x.UtilityTypeId,
                        principalTable: "UtilityTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UtilityMeterReadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UtilityCustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    UtilityTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReadingDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReadingValue = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PreviousReadingValue = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Consumption = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IsBackdated = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecalculationBatchId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilityMeterReadings", x => x.Id);
                    table.CheckConstraint("CK_UtilityMeterReadings_Consumption_NonNegative", "Consumption >= 0");
                    table.CheckConstraint("CK_UtilityMeterReadings_PreviousReading_NonNegative", "PreviousReadingValue >= 0");
                    table.CheckConstraint("CK_UtilityMeterReadings_Reading_NonNegative", "ReadingValue >= 0");
                    table.ForeignKey(
                        name: "FK_UtilityMeterReadings_UtilityCustomers_UtilityCustomerId",
                        column: x => x.UtilityCustomerId,
                        principalTable: "UtilityCustomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UtilityMeterReadings_UtilityTypes_UtilityTypeId",
                        column: x => x.UtilityTypeId,
                        principalTable: "UtilityTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UtilityRecalculationBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UtilityCustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    UtilityTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    TriggerReadingId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilityRecalculationBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UtilityRecalculationBatches_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UtilityRecalculationBatches_UtilityCustomers_UtilityCustomerId",
                        column: x => x.UtilityCustomerId,
                        principalTable: "UtilityCustomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UtilityRecalculationBatches_UtilityMeterReadings_TriggerReadingId",
                        column: x => x.TriggerReadingId,
                        principalTable: "UtilityMeterReadings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UtilityRecalculationBatches_UtilityTypes_UtilityTypeId",
                        column: x => x.UtilityTypeId,
                        principalTable: "UtilityTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UtilityAuditLogs_CorrelationId",
                table: "UtilityAuditLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityAuditLogs_EntityName_EntityId",
                table: "UtilityAuditLogs",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_UtilityBillPayments_PaymentDate",
                table: "UtilityBillPayments",
                column: "PaymentDate");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityBillPayments_UtilityBillId",
                table: "UtilityBillPayments",
                column: "UtilityBillId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityBillResponsibilities_TenantId",
                table: "UtilityBillResponsibilities",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityBillResponsibilities_UtilityBillId_TenantId_FromDate_ToDate",
                table: "UtilityBillResponsibilities",
                columns: new[] { "UtilityBillId", "TenantId", "FromDate", "ToDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UtilityBills_CreatedFromReadingId",
                table: "UtilityBills",
                column: "CreatedFromReadingId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityBills_DueDate",
                table: "UtilityBills",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityBills_RecalculationBatchId",
                table: "UtilityBills",
                column: "RecalculationBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityBills_Status",
                table: "UtilityBills",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityBills_UtilityCustomerId_UtilityTypeId_BillingPeriod",
                table: "UtilityBills",
                columns: new[] { "UtilityCustomerId", "UtilityTypeId", "BillingPeriod" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UtilityBills_UtilityTypeId",
                table: "UtilityBills",
                column: "UtilityTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityCustomerCredits_SourcePaymentId",
                table: "UtilityCustomerCredits",
                column: "SourcePaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityCustomerCredits_UtilityCustomerId_UtilityTypeId_OccurredAt",
                table: "UtilityCustomerCredits",
                columns: new[] { "UtilityCustomerId", "UtilityTypeId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UtilityCustomerCredits_UtilityTypeId",
                table: "UtilityCustomerCredits",
                column: "UtilityTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityCustomerRates_UtilityCustomerId_UtilityTypeId_BillingPeriod",
                table: "UtilityCustomerRates",
                columns: new[] { "UtilityCustomerId", "UtilityTypeId", "BillingPeriod" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UtilityCustomerRates_UtilityTypeId",
                table: "UtilityCustomerRates",
                column: "UtilityTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityCustomers_CustomerType",
                table: "UtilityCustomers",
                column: "CustomerType");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityCustomers_Name",
                table: "UtilityCustomers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityCustomers_RoomId",
                table: "UtilityCustomers",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityCustomers_TenantId",
                table: "UtilityCustomers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityMeterReadings_RecalculationBatchId",
                table: "UtilityMeterReadings",
                column: "RecalculationBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityMeterReadings_UtilityCustomerId_UtilityTypeId_ReadingDate",
                table: "UtilityMeterReadings",
                columns: new[] { "UtilityCustomerId", "UtilityTypeId", "ReadingDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UtilityMeterReadings_UtilityTypeId",
                table: "UtilityMeterReadings",
                column: "UtilityTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityRecalculationBatches_RequestedByUserId",
                table: "UtilityRecalculationBatches",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityRecalculationBatches_TriggerReadingId",
                table: "UtilityRecalculationBatches",
                column: "TriggerReadingId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityRecalculationBatches_UtilityCustomerId_UtilityTypeId_StartedAt",
                table: "UtilityRecalculationBatches",
                columns: new[] { "UtilityCustomerId", "UtilityTypeId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UtilityRecalculationBatches_UtilityTypeId",
                table: "UtilityRecalculationBatches",
                column: "UtilityTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityTypes_Code",
                table: "UtilityTypes",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UtilityBillPayments_UtilityBills_UtilityBillId",
                table: "UtilityBillPayments",
                column: "UtilityBillId",
                principalTable: "UtilityBills",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UtilityBillResponsibilities_UtilityBills_UtilityBillId",
                table: "UtilityBillResponsibilities",
                column: "UtilityBillId",
                principalTable: "UtilityBills",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UtilityBills_UtilityMeterReadings_CreatedFromReadingId",
                table: "UtilityBills",
                column: "CreatedFromReadingId",
                principalTable: "UtilityMeterReadings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UtilityBills_UtilityRecalculationBatches_RecalculationBatchId",
                table: "UtilityBills",
                column: "RecalculationBatchId",
                principalTable: "UtilityRecalculationBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UtilityMeterReadings_UtilityRecalculationBatches_RecalculationBatchId",
                table: "UtilityMeterReadings",
                column: "RecalculationBatchId",
                principalTable: "UtilityRecalculationBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UtilityMeterReadings_UtilityCustomers_UtilityCustomerId",
                table: "UtilityMeterReadings");

            migrationBuilder.DropForeignKey(
                name: "FK_UtilityRecalculationBatches_UtilityCustomers_UtilityCustomerId",
                table: "UtilityRecalculationBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_UtilityRecalculationBatches_UtilityMeterReadings_TriggerReadingId",
                table: "UtilityRecalculationBatches");

            migrationBuilder.DropTable(
                name: "UtilityAuditLogs");

            migrationBuilder.DropTable(
                name: "UtilityBillResponsibilities");

            migrationBuilder.DropTable(
                name: "UtilityCustomerCredits");

            migrationBuilder.DropTable(
                name: "UtilityCustomerRates");

            migrationBuilder.DropTable(
                name: "UtilityBillPayments");

            migrationBuilder.DropTable(
                name: "UtilityBills");

            migrationBuilder.DropTable(
                name: "UtilityCustomers");

            migrationBuilder.DropTable(
                name: "UtilityMeterReadings");

            migrationBuilder.DropTable(
                name: "UtilityRecalculationBatches");

            migrationBuilder.DropTable(
                name: "UtilityTypes");
        }
    }
}
