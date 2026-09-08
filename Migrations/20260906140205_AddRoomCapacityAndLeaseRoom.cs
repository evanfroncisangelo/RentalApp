using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalApp.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomCapacityAndLeaseRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoomId",
                table: "Leases",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UnitId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoomNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    MaxCapacity = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rooms_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(@"
                INSERT INTO Rooms (UnitId, RoomNumber, MaxCapacity, IsActive, CreatedAt, UpdatedAt)
                SELECT Id, 'Room 1', 3, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                FROM Units;

                INSERT INTO Rooms (UnitId, RoomNumber, MaxCapacity, IsActive, CreatedAt, UpdatedAt)
                SELECT Id, 'Room 2', 3, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                FROM Units;

                UPDATE Leases
                SET RoomId = (
                    SELECT r.Id
                    FROM Rooms r
                    WHERE r.UnitId = Leases.UnitId AND r.RoomNumber = 'Room 1'
                    LIMIT 1
                )
                WHERE RoomId = 0;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Leases_RoomId_Status",
                table: "Leases",
                columns: new[] { "RoomId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_UnitId_RoomNumber",
                table: "Rooms",
                columns: new[] { "UnitId", "RoomNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Leases_Rooms_RoomId",
                table: "Leases",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leases_Rooms_RoomId",
                table: "Leases");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Leases_RoomId_Status",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "Leases");
        }
    }
}
