using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RentalApp.Data;

#nullable disable

namespace RentalApp.Migrations
{
    [DbContext(typeof(RentalDbContext))]
    [Migration("20260910030000_SetUnitMaxCapacityDefaultToZero")]
    public partial class SetUnitMaxCapacityDefaultToZero : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
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
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
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

ALTER TABLE [dbo].[Units] ADD DEFAULT (1) FOR [MaxCapacity];
");
        }
    }
}
