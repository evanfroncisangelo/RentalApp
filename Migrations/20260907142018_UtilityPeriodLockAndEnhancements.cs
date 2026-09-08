using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalApp.Migrations
{
    /// <inheritdoc />
    public partial class UtilityPeriodLockAndEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UtilityBillingPeriodLocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UtilityTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    UtilityCustomerId = table.Column<int>(type: "INTEGER", nullable: true),
                    BillingPeriod = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    IsLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_UtilityBillingPeriodLocks_UtilityCustomerId",
                table: "UtilityBillingPeriodLocks",
                column: "UtilityCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityBillingPeriodLocks_UtilityTypeId_UtilityCustomerId_BillingPeriod",
                table: "UtilityBillingPeriodLocks",
                columns: new[] { "UtilityTypeId", "UtilityCustomerId", "BillingPeriod" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UtilityBillingPeriodLocks");
        }
    }
}
