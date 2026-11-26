using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTrackingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddTotalHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalHours",
                table: "TimeEntries",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalHours",
                table: "TimeEntries");
        }
    }
}
