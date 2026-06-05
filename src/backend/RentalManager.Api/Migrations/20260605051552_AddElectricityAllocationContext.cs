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
            migrationBuilder.Sql("""
                CREATE TABLE "ef_temp_ElectricityAllocations" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_ElectricityAllocations" PRIMARY KEY AUTOINCREMENT,
                    "ElectricityBillId" INTEGER NOT NULL,
                    "ContractId" INTEGER NOT NULL,
                    "PropertyRoomId" INTEGER NOT NULL,
                    "TenantId" INTEGER NULL,
                    "OccupancyStartUtc" TEXT NOT NULL,
                    "OccupancyEndUtc" TEXT NOT NULL,
                    "MeterStart" TEXT NULL,
                    "MeterEnd" TEXT NULL,
                    "TenantUnits" TEXT NOT NULL,
                    "OccupantCount" INTEGER NOT NULL,
                    "OccupancyDays" INTEGER NOT NULL,
                    "PrivateAmount" TEXT NOT NULL,
                    "PublicAmount" TEXT NOT NULL,
                    "PayableAmount" TEXT NOT NULL,
                    CONSTRAINT "FK_ElectricityAllocations_Contracts_ContractId" FOREIGN KEY ("ContractId") REFERENCES "Contracts" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_ElectricityAllocations_ElectricityBills_ElectricityBillId" FOREIGN KEY ("ElectricityBillId") REFERENCES "ElectricityBills" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_ElectricityAllocations_PropertyRooms_PropertyRoomId" FOREIGN KEY ("PropertyRoomId") REFERENCES "PropertyRooms" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_ElectricityAllocations_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE SET NULL
                );
                """);

            migrationBuilder.Sql("""
                INSERT INTO "ef_temp_ElectricityAllocations" (
                    "Id", "ElectricityBillId", "ContractId", "PropertyRoomId", "TenantId",
                    "OccupancyStartUtc", "OccupancyEndUtc", "MeterStart", "MeterEnd",
                    "TenantUnits", "OccupantCount", "OccupancyDays", "PrivateAmount", "PublicAmount", "PayableAmount"
                )
                SELECT
                    ea."Id",
                    ea."ElectricityBillId",
                    eb."ContractId",
                    COALESCE((
                        SELECT cr."PropertyRoomId"
                        FROM "ContractRooms" cr
                        WHERE cr."ContractId" = eb."ContractId"
                        ORDER BY cr."Id"
                        LIMIT 1
                    ), 0),
                    ea."TenantId",
                    eb."BillingStartUtc",
                    eb."BillingEndUtc",
                    NULL,
                    NULL,
                    ea."TenantUnits",
                    ea."OccupantCount",
                    ea."OccupancyDays",
                    ea."PrivateAmount",
                    ea."PublicAmount",
                    ea."PayableAmount"
                FROM "ElectricityAllocations" ea
                INNER JOIN "ElectricityBills" eb ON eb."Id" = ea."ElectricityBillId";
                """);

            migrationBuilder.Sql(@"DROP TABLE ""ElectricityAllocations"";");
            migrationBuilder.Sql(@"ALTER TABLE ""ef_temp_ElectricityAllocations"" RENAME TO ""ElectricityAllocations"";");

            migrationBuilder.CreateIndex(
                name: "IX_ElectricityAllocations_ContractId",
                table: "ElectricityAllocations",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ElectricityAllocations_ElectricityBillId",
                table: "ElectricityAllocations",
                column: "ElectricityBillId");

            migrationBuilder.CreateIndex(
                name: "IX_ElectricityAllocations_PropertyRoomId",
                table: "ElectricityAllocations",
                column: "PropertyRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_ElectricityAllocations_TenantId",
                table: "ElectricityAllocations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ElectricityMeterReadings_PropertyRoomId_ReadingDateUtc",
                table: "ElectricityMeterReadings",
                columns: new[] { "PropertyRoomId", "ReadingDateUtc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ElectricityAllocations_ContractId",
                table: "ElectricityAllocations");

            migrationBuilder.DropIndex(
                name: "IX_ElectricityAllocations_ElectricityBillId",
                table: "ElectricityAllocations");

            migrationBuilder.DropIndex(
                name: "IX_ElectricityAllocations_PropertyRoomId",
                table: "ElectricityAllocations");

            migrationBuilder.DropIndex(
                name: "IX_ElectricityAllocations_TenantId",
                table: "ElectricityAllocations");

            migrationBuilder.DropIndex(
                name: "IX_ElectricityMeterReadings_PropertyRoomId_ReadingDateUtc",
                table: "ElectricityMeterReadings");

            migrationBuilder.Sql("""
                CREATE TABLE "ef_temp_ElectricityAllocations" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_ElectricityAllocations" PRIMARY KEY AUTOINCREMENT,
                    "ElectricityBillId" INTEGER NOT NULL,
                    "TenantId" INTEGER NULL,
                    "TenantUnits" TEXT NOT NULL,
                    "OccupantCount" INTEGER NOT NULL,
                    "OccupancyDays" INTEGER NOT NULL,
                    "PrivateAmount" TEXT NOT NULL,
                    "PublicAmount" TEXT NOT NULL,
                    "PayableAmount" TEXT NOT NULL,
                    CONSTRAINT "FK_ElectricityAllocations_ElectricityBills_ElectricityBillId" FOREIGN KEY ("ElectricityBillId") REFERENCES "ElectricityBills" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_ElectricityAllocations_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE SET NULL
                );
                """);

            migrationBuilder.Sql("""
                INSERT INTO "ef_temp_ElectricityAllocations" (
                    "Id", "ElectricityBillId", "TenantId", "TenantUnits", "OccupantCount",
                    "OccupancyDays", "PrivateAmount", "PublicAmount", "PayableAmount"
                )
                SELECT
                    "Id", "ElectricityBillId", "TenantId", "TenantUnits", "OccupantCount",
                    "OccupancyDays", "PrivateAmount", "PublicAmount", "PayableAmount"
                FROM "ElectricityAllocations";
                """);

            migrationBuilder.Sql(@"DROP TABLE ""ElectricityAllocations"";");
            migrationBuilder.Sql(@"ALTER TABLE ""ef_temp_ElectricityAllocations"" RENAME TO ""ElectricityAllocations"";");

            migrationBuilder.CreateIndex(
                name: "IX_ElectricityAllocations_ElectricityBillId",
                table: "ElectricityAllocations",
                column: "ElectricityBillId");

            migrationBuilder.CreateIndex(
                name: "IX_ElectricityAllocations_TenantId",
                table: "ElectricityAllocations",
                column: "TenantId");
        }
    }
}
