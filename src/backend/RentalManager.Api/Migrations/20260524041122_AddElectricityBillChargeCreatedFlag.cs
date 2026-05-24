using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddElectricityBillChargeCreatedFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ChargesCreated",
                table: "ElectricityBills",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ChargesCreatedAtUtc",
                table: "ElectricityBills",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AdminUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAtUtc", "PasswordHash" },
                values: new object[] { new DateTime(2026, 5, 24, 4, 11, 21, 638, DateTimeKind.Utc).AddTicks(8194), "$2a$11$HlDgIuiyVxHd356KWpNR4OtSocmp5RQNYprtQXuKFl6jBlbaRkq3a" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChargesCreated",
                table: "ElectricityBills");

            migrationBuilder.DropColumn(
                name: "ChargesCreatedAtUtc",
                table: "ElectricityBills");

            migrationBuilder.UpdateData(
                table: "AdminUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAtUtc", "PasswordHash" },
                values: new object[] { new DateTime(2026, 5, 24, 4, 1, 49, 214, DateTimeKind.Utc).AddTicks(9993), "$2a$11$5TybUTw.Srg9q2Mg6qkSieG.xA/q3Sl2hRLasR30BuPOD7ClgAx1y" });
        }
    }
}
