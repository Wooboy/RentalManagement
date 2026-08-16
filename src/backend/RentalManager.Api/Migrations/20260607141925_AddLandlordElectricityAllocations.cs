using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLandlordElectricityAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ElectricityAllocations_Contracts_ContractId",
                table: "ElectricityAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_ElectricityBills_Contracts_ContractId",
                table: "ElectricityBills");

            migrationBuilder.AlterColumn<int>(
                name: "ContractId",
                table: "ElectricityBills",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "ContractId",
                table: "ElectricityAllocations",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "TargetType",
                table: "ElectricityAllocations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_ElectricityAllocations_Contracts_ContractId",
                table: "ElectricityAllocations",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ElectricityBills_Contracts_ContractId",
                table: "ElectricityBills",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ElectricityAllocations_Contracts_ContractId",
                table: "ElectricityAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_ElectricityBills_Contracts_ContractId",
                table: "ElectricityBills");

            migrationBuilder.DropColumn(
                name: "TargetType",
                table: "ElectricityAllocations");

            migrationBuilder.AlterColumn<int>(
                name: "ContractId",
                table: "ElectricityBills",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ContractId",
                table: "ElectricityAllocations",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ElectricityAllocations_Contracts_ContractId",
                table: "ElectricityAllocations",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ElectricityBills_Contracts_ContractId",
                table: "ElectricityBills",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
