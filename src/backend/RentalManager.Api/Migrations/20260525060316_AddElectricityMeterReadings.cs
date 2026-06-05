using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddElectricityMeterReadings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ElectricityMeterReadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PropertyUnitId = table.Column<int>(type: "INTEGER", nullable: false),
                    PropertyRoomId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReadingDateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReadingValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectricityMeterReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ElectricityMeterReadings_PropertyRooms_PropertyRoomId",
                        column: x => x.PropertyRoomId,
                        principalTable: "PropertyRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ElectricityMeterReadings_PropertyUnits_PropertyUnitId",
                        column: x => x.PropertyUnitId,
                        principalTable: "PropertyUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ElectricityMeterReadings_PropertyRoomId",
                table: "ElectricityMeterReadings",
                column: "PropertyRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_ElectricityMeterReadings_PropertyUnitId",
                table: "ElectricityMeterReadings",
                column: "PropertyUnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ElectricityMeterReadings");
        }
    }
}
