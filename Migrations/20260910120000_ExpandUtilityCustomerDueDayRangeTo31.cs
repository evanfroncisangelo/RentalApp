using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalApp.Migrations
{
    /// <inheritdoc />
    public partial class ExpandUtilityCustomerDueDayRangeTo31 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_UtilityCustomers_DueDay_Range'
      AND parent_object_id = OBJECT_ID('dbo.UtilityCustomers'))
BEGIN
    ALTER TABLE [dbo].[UtilityCustomers] DROP CONSTRAINT [CK_UtilityCustomers_DueDay_Range];
END
");

            migrationBuilder.Sql(@"
ALTER TABLE [dbo].[UtilityCustomers]
ADD CONSTRAINT [CK_UtilityCustomers_DueDay_Range]
CHECK ([DueDayOfMonth] IS NULL OR ([DueDayOfMonth] >= 1 AND [DueDayOfMonth] <= 31));
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_UtilityCustomers_DueDay_Range'
      AND parent_object_id = OBJECT_ID('dbo.UtilityCustomers'))
BEGIN
    ALTER TABLE [dbo].[UtilityCustomers] DROP CONSTRAINT [CK_UtilityCustomers_DueDay_Range];
END
");

            migrationBuilder.Sql(@"
ALTER TABLE [dbo].[UtilityCustomers]
ADD CONSTRAINT [CK_UtilityCustomers_DueDay_Range]
CHECK ([DueDayOfMonth] IS NULL OR ([DueDayOfMonth] >= 1 AND [DueDayOfMonth] <= 28));
");
        }
    }
}
