using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AlignLatestModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "UsageUnits",
                table: "ExpenseRecords",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsageUnits",
                table: "ExpenseRecords");
        }
    }
}
