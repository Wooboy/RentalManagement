using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddElectricityBillStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ElectricityBills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContractId = table.Column<int>(type: "INTEGER", nullable: false),
                    RuleType = table.Column<int>(type: "INTEGER", nullable: false),
                    BillingStartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BillingEndUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalUnits = table.Column<decimal>(type: "TEXT", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    PrivateTotalAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    PublicTotalAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    PayableTotalAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectricityBills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ElectricityBills_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ElectricityAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ElectricityBillId = table.Column<int>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: true),
                    TenantUnits = table.Column<decimal>(type: "TEXT", nullable: false),
                    OccupantCount = table.Column<int>(type: "INTEGER", nullable: false),
                    OccupancyDays = table.Column<int>(type: "INTEGER", nullable: false),
                    PrivateAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    PublicAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    PayableAmount = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectricityAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ElectricityAllocations_ElectricityBills_ElectricityBillId",
                        column: x => x.ElectricityBillId,
                        principalTable: "ElectricityBills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ElectricityAllocations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.UpdateData(
                table: "AdminUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAtUtc", "PasswordHash" },
                values: new object[] { new DateTime(2026, 5, 24, 4, 1, 49, 214, DateTimeKind.Utc).AddTicks(9993), "$2a$11$5TybUTw.Srg9q2Mg6qkSieG.xA/q3Sl2hRLasR30BuPOD7ClgAx1y" });

            migrationBuilder.CreateIndex(
                name: "IX_ElectricityAllocations_ElectricityBillId",
                table: "ElectricityAllocations",
                column: "ElectricityBillId");

            migrationBuilder.CreateIndex(
                name: "IX_ElectricityAllocations_TenantId",
                table: "ElectricityAllocations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ElectricityBills_ContractId",
                table: "ElectricityBills",
                column: "ContractId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ElectricityAllocations");

            migrationBuilder.DropTable(
                name: "ElectricityBills");

            migrationBuilder.UpdateData(
                table: "AdminUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAtUtc", "PasswordHash" },
                values: new object[] { new DateTime(2026, 5, 24, 3, 45, 34, 225, DateTimeKind.Utc).AddTicks(4269), "$2a$11$MwEuGkUTNc2sU52LKaYQvuSM/UMIOb5Ue2xju9IHNLgIbP.ygHpWe" });
        }
    }
}
