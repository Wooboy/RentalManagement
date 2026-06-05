using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddContractNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Contracts",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Contracts");
        }
    }
}
