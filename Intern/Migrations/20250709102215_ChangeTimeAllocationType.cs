using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Intern.Migrations
{
    public partial class ChangeTimeAllocationType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the default constraint (in case exists)
            migrationBuilder.Sql(@"
        DECLARE @constraintName NVARCHAR(255);
        SELECT @constraintName = dc.name
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
        WHERE dc.parent_object_id = OBJECT_ID('Agenda') AND c.name = 'TimeAllocation';

        IF @constraintName IS NOT NULL
            EXEC('ALTER TABLE Agenda DROP CONSTRAINT [' + @constraintName + ']');
    ");

            // Drop and re-add the column
            migrationBuilder.DropColumn(
                name: "TimeAllocation",
                table: "Agenda"
            );

            migrationBuilder.AddColumn<int>(
                name: "TimeAllocation",
                table: "Agenda",
                type: "int",
                nullable: false,
                defaultValue: 0 // or another suitable default
            );
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeAllocation",
                table: "Agenda"
            );

            migrationBuilder.AddColumn<TimeOnly>(
                name: "TimeAllocation",
                table: "Agenda",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0) // adjust as needed
            );
        }

    }
}
