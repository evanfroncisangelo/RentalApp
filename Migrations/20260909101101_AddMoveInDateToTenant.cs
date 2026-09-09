using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalApp.Migrations
{
    /// <inheritdoc />
    public partial class AddMoveInDateToTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "MoveInDate",
                table: "Tenants",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MoveInDate",
                table: "Tenants");
        }
    }
}
