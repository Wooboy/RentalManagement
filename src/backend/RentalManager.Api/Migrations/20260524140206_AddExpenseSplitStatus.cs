using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseSplitStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SplitStatus",
                table: "ExpenseRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SplitStatus",
                table: "ExpenseRecords");
        }
    }
}
