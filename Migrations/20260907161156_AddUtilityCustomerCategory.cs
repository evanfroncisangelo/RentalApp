using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalApp.Migrations
{
    /// <inheritdoc />
    public partial class AddUtilityCustomerCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UtilityCategoryId",
                table: "UtilityCustomers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UtilityCustomers_UtilityCategoryId",
                table: "UtilityCustomers",
                column: "UtilityCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_UtilityCustomers_UtilityTypes_UtilityCategoryId",
                table: "UtilityCustomers",
                column: "UtilityCategoryId",
                principalTable: "UtilityTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UtilityCustomers_UtilityTypes_UtilityCategoryId",
                table: "UtilityCustomers");

            migrationBuilder.DropIndex(
                name: "IX_UtilityCustomers_UtilityCategoryId",
                table: "UtilityCustomers");

            migrationBuilder.DropColumn(
                name: "UtilityCategoryId",
                table: "UtilityCustomers");
        }
    }
}
