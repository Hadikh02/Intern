using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Intern.Migrations
{
    /// <inheritdoc />
    public partial class RemoveThreeColumnsFromAttendee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasAudio",
                table: "MeetingAttendee");

            migrationBuilder.DropColumn(
                name: "HasVideo",
                table: "MeetingAttendee");

            migrationBuilder.DropColumn(
                name: "IsHandRaised",
                table: "MeetingAttendee");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasAudio",
                table: "MeetingAttendee",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasVideo",
                table: "MeetingAttendee",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsHandRaised",
                table: "MeetingAttendee",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
