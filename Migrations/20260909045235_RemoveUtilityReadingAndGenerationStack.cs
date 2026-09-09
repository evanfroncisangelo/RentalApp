using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalApp.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUtilityReadingAndGenerationStack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("PRAGMA foreign_keys = OFF;");

            migrationBuilder.Sql(@"
CREATE TABLE ""ef_temp_UtilityBills"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_UtilityBills"" PRIMARY KEY AUTOINCREMENT,
    ""UtilityCustomerId"" INTEGER NOT NULL,
    ""UtilityTypeId"" INTEGER NOT NULL,
    ""BillingPeriod"" TEXT NOT NULL,
    ""Amount"" decimal(18,2) NOT NULL,
    ""DueDate"" TEXT NULL,
    ""Status"" INTEGER NOT NULL,
    ""Version"" BLOB NOT NULL,
    ""CreatedAt"" TEXT NOT NULL,
    ""UpdatedAt"" TEXT NOT NULL,
    CONSTRAINT ""FK_UtilityBills_UtilityCustomers_UtilityCustomerId"" FOREIGN KEY (""UtilityCustomerId"") REFERENCES ""UtilityCustomers"" (""Id"") ON DELETE RESTRICT,
    CONSTRAINT ""FK_UtilityBills_UtilityTypes_UtilityTypeId"" FOREIGN KEY (""UtilityTypeId"") REFERENCES ""UtilityTypes"" (""Id"") ON DELETE RESTRICT,
    CONSTRAINT ""CK_UtilityBills_Amount_NonNegative"" CHECK (Amount >= 0),
    CONSTRAINT ""CK_UtilityBills_BillingPeriod_Length"" CHECK (length(BillingPeriod) = 7)
);");

            migrationBuilder.Sql(@"
INSERT INTO ""ef_temp_UtilityBills"" (
    ""Id"", ""UtilityCustomerId"", ""UtilityTypeId"", ""BillingPeriod"", ""Amount"", ""DueDate"", ""Status"", ""Version"", ""CreatedAt"", ""UpdatedAt""
)
SELECT
    ""Id"", ""UtilityCustomerId"", ""UtilityTypeId"", ""BillingPeriod"", ""Amount"", ""DueDate"", ""Status"", ""Version"", ""CreatedAt"", ""UpdatedAt""
FROM ""UtilityBills"";");

            migrationBuilder.Sql("DROP TABLE \"UtilityBills\";");
            migrationBuilder.Sql("ALTER TABLE \"ef_temp_UtilityBills\" RENAME TO \"UtilityBills\";");

            migrationBuilder.Sql("CREATE UNIQUE INDEX \"IX_UtilityBills_UtilityCustomerId_UtilityTypeId_BillingPeriod\" ON \"UtilityBills\" (\"UtilityCustomerId\", \"UtilityTypeId\", \"BillingPeriod\");");
            migrationBuilder.Sql("CREATE INDEX \"IX_UtilityBills_Status\" ON \"UtilityBills\" (\"Status\");");
            migrationBuilder.Sql("CREATE INDEX \"IX_UtilityBills_DueDate\" ON \"UtilityBills\" (\"DueDate\");");
            migrationBuilder.Sql("CREATE INDEX \"IX_UtilityBills_UtilityTypeId\" ON \"UtilityBills\" (\"UtilityTypeId\");");

            migrationBuilder.Sql("DROP TABLE IF EXISTS \"UtilityBillingPeriodLocks\";");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"UtilityBillResponsibilities\";");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"UtilityCustomerRates\";");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"UtilityMeterReadings\";");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"UtilityRecalculationBatches\";");

            migrationBuilder.Sql("PRAGMA foreign_keys = ON;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Consumption",
                table: "UtilityBills",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CreatedFromReadingId",
                table: "UtilityBills",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentReading",
                table: "UtilityBills",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsRecalculated",
                table: "UtilityBills",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PreviousReading",
                table: "UtilityBills",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Rate",
                table: "UtilityBills",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "RecalculationBatchId",
                table: "UtilityBills",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UtilityBillingPeriodLocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UtilityCustomerId = table.Column<int>(type: "INTEGER", nullable: true),
                    UtilityTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    BillingPeriod = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilityBillingPeriodLocks", x => x.Id);
                    table.CheckConstraint("CK_UtilityBillingPeriodLocks_BillingPeriod_Length", "length(BillingPeriod) = 7");
                    table.ForeignKey(
                        name: "FK_UtilityBillingPeriodLocks_UtilityCustomers_UtilityCustomerId",
                        column: x => x.UtilityCustomerId,
                        principalTable: "UtilityCustomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UtilityBillingPeriodLocks_UtilityTypes_UtilityTypeId",
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
                    TenantId = table.Column<int>(type: "INTEGER", nullable: true),
                    UtilityBillId = table.Column<int>(type: "INTEGER", nullable: false),
                    AmountShare = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DaysCovered = table.Column<int>(type: "INTEGER", nullable: false),
                    FromDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PercentageShare = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    ToDate = table.Column<DateTime>(type: "TEXT", nullable: false),
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
                    table.ForeignKey(
                        name: "FK_UtilityBillResponsibilities_UtilityBills_UtilityBillId",
                        column: x => x.UtilityBillId,
                        principalTable: "UtilityBills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
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
                name: "UtilityMeterReadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RecalculationBatchId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UtilityCustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    UtilityTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Consumption = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsBackdated = table.Column<bool>(type: "INTEGER", nullable: false),
                    PreviousReadingValue = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReadingDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReadingValue = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
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
                    RequestedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    TriggerReadingId = table.Column<int>(type: "INTEGER", nullable: false),
                    UtilityCustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    UtilityTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
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
                name: "IX_UtilityBills_CreatedFromReadingId",
                table: "UtilityBills",
                column: "CreatedFromReadingId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityBills_RecalculationBatchId",
                table: "UtilityBills",
                column: "RecalculationBatchId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UtilityBills_Consumption_NonNegative",
                table: "UtilityBills",
                sql: "Consumption >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UtilityBills_Rate_NonNegative",
                table: "UtilityBills",
                sql: "Rate >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityBillingPeriodLocks_UtilityCustomerId",
                table: "UtilityBillingPeriodLocks",
                column: "UtilityCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityBillingPeriodLocks_UtilityTypeId_UtilityCustomerId_BillingPeriod",
                table: "UtilityBillingPeriodLocks",
                columns: new[] { "UtilityTypeId", "UtilityCustomerId", "BillingPeriod" },
                unique: true);

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
                name: "IX_UtilityCustomerRates_UtilityCustomerId_UtilityTypeId_BillingPeriod",
                table: "UtilityCustomerRates",
                columns: new[] { "UtilityCustomerId", "UtilityTypeId", "BillingPeriod" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UtilityCustomerRates_UtilityTypeId",
                table: "UtilityCustomerRates",
                column: "UtilityTypeId");

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
    }
}
