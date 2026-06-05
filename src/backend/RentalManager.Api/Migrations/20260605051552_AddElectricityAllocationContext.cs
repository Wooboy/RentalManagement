using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddElectricityAllocationContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ContractId",
                table: "ElectricityAllocations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MeterEnd",
                table: "ElectricityAllocations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MeterStart",
                table: "ElectricityAllocations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OccupancyEndUtc",
                table: "ElectricityAllocations",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "OccupancyStartUtc",
                table: "ElectricityAllocations",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<int>(
                name: "PropertyRoomId",
                table: "ElectricityAllocations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE ElectricityAllocations
                SET ContractId = (
                    SELECT ContractId
                    FROM ElectricityBills
                    WHERE ElectricityBills.Id = ElectricityAllocations.ElectricityBillId
                ),
                OccupancyStartUtc = (
                    SELECT BillingStartUtc
                    FROM ElectricityBills
                    WHERE ElectricityBills.Id = ElectricityAllocations.ElectricityBillId
                ),
                OccupancyEndUtc = (
                    SELECT BillingEndUtc
                    FROM ElectricityBills
                    WHERE ElectricityBills.Id = ElectricityAllocations.ElectricityBillId
                ),
                PropertyRoomId = COALESCE((
                    SELECT ContractRooms.PropertyRoomId
                    FROM ElectricityBills
                    INNER JOIN ContractRooms ON ContractRooms.ContractId = ElectricityBills.ContractId
                    WHERE ElectricityBills.Id = ElectricityAllocations.ElectricityBillId
                    ORDER BY ContractRooms.Id
                    LIMIT 1
                ), 0)
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ElectricityAllocations_ContractId",
                table: "ElectricityAllocations",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ElectricityAllocations_PropertyRoomId",
                table: "ElectricityAllocations",
                column: "PropertyRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_ElectricityMeterReadings_PropertyRoomId_ReadingDateUtc",
                table: "ElectricityMeterReadings",
                columns: new[] { "PropertyRoomId", "ReadingDateUtc" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ElectricityAllocations_Contracts_ContractId",
                table: "ElectricityAllocations",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ElectricityAllocations_PropertyRooms_PropertyRoomId",
                table: "ElectricityAllocations",
                column: "PropertyRoomId",
                principalTable: "PropertyRooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ElectricityAllocations_Contracts_ContractId",
                table: "ElectricityAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_ElectricityAllocations_PropertyRooms_PropertyRoomId",
                table: "ElectricityAllocations");

            migrationBuilder.DropIndex(
                name: "IX_ElectricityAllocations_ContractId",
                table: "ElectricityAllocations");

            migrationBuilder.DropIndex(
                name: "IX_ElectricityAllocations_PropertyRoomId",
                table: "ElectricityAllocations");

            migrationBuilder.DropIndex(
                name: "IX_ElectricityMeterReadings_PropertyRoomId_ReadingDateUtc",
                table: "ElectricityMeterReadings");

            migrationBuilder.DropColumn(
                name: "ContractId",
                table: "ElectricityAllocations");

            migrationBuilder.DropColumn(
                name: "MeterEnd",
                table: "ElectricityAllocations");

            migrationBuilder.DropColumn(
                name: "MeterStart",
                table: "ElectricityAllocations");

            migrationBuilder.DropColumn(
                name: "OccupancyEndUtc",
                table: "ElectricityAllocations");

            migrationBuilder.DropColumn(
                name: "OccupancyStartUtc",
                table: "ElectricityAllocations");

            migrationBuilder.DropColumn(
                name: "PropertyRoomId",
                table: "ElectricityAllocations");
        }
    }
}
