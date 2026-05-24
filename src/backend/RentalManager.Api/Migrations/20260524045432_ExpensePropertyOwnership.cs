using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class ExpensePropertyOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PropertyRoomId",
                table: "ExpenseRecords",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PropertyUnitId",
                table: "ExpenseRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRecords_PropertyRoomId",
                table: "ExpenseRecords",
                column: "PropertyRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRecords_PropertyUnitId",
                table: "ExpenseRecords",
                column: "PropertyUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseRecords_PropertyRooms_PropertyRoomId",
                table: "ExpenseRecords",
                column: "PropertyRoomId",
                principalTable: "PropertyRooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseRecords_PropertyUnits_PropertyUnitId",
                table: "ExpenseRecords",
                column: "PropertyUnitId",
                principalTable: "PropertyUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseRecords_PropertyRooms_PropertyRoomId",
                table: "ExpenseRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseRecords_PropertyUnits_PropertyUnitId",
                table: "ExpenseRecords");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseRecords_PropertyRoomId",
                table: "ExpenseRecords");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseRecords_PropertyUnitId",
                table: "ExpenseRecords");

            migrationBuilder.DropColumn(
                name: "PropertyRoomId",
                table: "ExpenseRecords");

            migrationBuilder.DropColumn(
                name: "PropertyUnitId",
                table: "ExpenseRecords");
        }
    }
}
