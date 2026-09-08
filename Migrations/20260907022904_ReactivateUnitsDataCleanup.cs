using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalApp.Migrations
{
    /// <inheritdoc />
    public partial class ReactivateUnitsDataCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RentalPrices");

            migrationBuilder.Sql(@"
                UPDATE Units
                SET IsActive = 1,
                    Status = CASE
                        WHEN EXISTS (
                            SELECT 1
                            FROM Leases l
                            WHERE l.UnitId = Units.Id AND l.Status = 1
                        ) THEN 2
                        ELSE 1
                    END,
                    UpdatedAt = CURRENT_TIMESTAMP
                WHERE IsActive = 0 OR Status = 4;

                UPDATE Rooms
                SET IsActive = 1,
                    UpdatedAt = CURRENT_TIMESTAMP
                WHERE IsActive = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RentalPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UnitId = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RentalPrices_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RentalPrices_UnitId_EffectiveFrom",
                table: "RentalPrices",
                columns: new[] { "UnitId", "EffectiveFrom" });
        }
    }
}
