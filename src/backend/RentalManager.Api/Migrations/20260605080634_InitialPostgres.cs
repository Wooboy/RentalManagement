using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RentalManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PropertyUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyUnits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    BirthdayUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TaxId = table.Column<string>(type: "text", nullable: true),
                    PersonalId = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    EmergencyContactName = table.Column<string>(type: "text", nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PropertyRooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PropertyUnitId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyRooms_PropertyUnits_PropertyUnitId",
                        column: x => x.PropertyUnitId,
                        principalTable: "PropertyUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Contracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContractNo = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    PropertyUnitId = table.Column<int>(type: "integer", nullable: true),
                    PropertyName = table.Column<string>(type: "text", nullable: false),
                    PropertyAddress = table.Column<string>(type: "text", nullable: false),
                    StartDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MonthlyRent = table.Column<decimal>(type: "numeric", nullable: false),
                    PaymentIntervalMonths = table.Column<int>(type: "integer", nullable: false),
                    PeriodPayableAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Deposit = table.Column<decimal>(type: "numeric", nullable: false),
                    OccupantCount = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    ElectricityRuleType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contracts_PropertyUnits_PropertyUnitId",
                        column: x => x.PropertyUnitId,
                        principalTable: "PropertyUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Contracts_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ElectricityMeterReadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PropertyUnitId = table.Column<int>(type: "integer", nullable: false),
                    PropertyRoomId = table.Column<int>(type: "integer", nullable: false),
                    ReadingDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadingValue = table.Column<decimal>(type: "numeric", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "ExpenseRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PropertyUnitId = table.Column<int>(type: "integer", nullable: false),
                    PropertyRoomId = table.Column<int>(type: "integer", nullable: true),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    BillingStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BillingEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    UsageUnits = table.Column<decimal>(type: "numeric", nullable: true),
                    SplitStatus = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseRecords_PropertyRooms_PropertyRoomId",
                        column: x => x.PropertyRoomId,
                        principalTable: "PropertyRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExpenseRecords_PropertyUnits_PropertyUnitId",
                        column: x => x.PropertyUnitId,
                        principalTable: "PropertyUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChargeRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContractId = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    BillingStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BillingEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MeterStart = table.Column<decimal>(type: "numeric", nullable: true),
                    MeterEnd = table.Column<decimal>(type: "numeric", nullable: true),
                    UsageUnits = table.Column<decimal>(type: "numeric", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsPaid = table.Column<bool>(type: "boolean", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargeRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChargeRecords_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractRooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContractId = table.Column<int>(type: "integer", nullable: false),
                    PropertyRoomId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractRooms_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractRooms_PropertyRooms_PropertyRoomId",
                        column: x => x.PropertyRoomId,
                        principalTable: "PropertyRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ElectricityBills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContractId = table.Column<int>(type: "integer", nullable: false),
                    RuleType = table.Column<int>(type: "integer", nullable: false),
                    BillingStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BillingEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalUnits = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    PrivateTotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    PublicTotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    PayableTotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ChargesCreated = table.Column<bool>(type: "boolean", nullable: false),
                    ChargesCreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ElectricityBillId = table.Column<int>(type: "integer", nullable: false),
                    ContractId = table.Column<int>(type: "integer", nullable: false),
                    PropertyRoomId = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: true),
                    OccupancyStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OccupancyEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MeterStart = table.Column<decimal>(type: "numeric", nullable: true),
                    MeterEnd = table.Column<decimal>(type: "numeric", nullable: true),
                    TenantUnits = table.Column<decimal>(type: "numeric", nullable: false),
                    OccupantCount = table.Column<int>(type: "integer", nullable: false),
                    OccupancyDays = table.Column<int>(type: "integer", nullable: false),
                    PrivateAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    PublicAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    PayableAmount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectricityAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ElectricityAllocations_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ElectricityAllocations_ElectricityBills_ElectricityBillId",
                        column: x => x.ElectricityBillId,
                        principalTable: "ElectricityBills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ElectricityAllocations_PropertyRooms_PropertyRoomId",
                        column: x => x.PropertyRoomId,
                        principalTable: "PropertyRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ElectricityAllocations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "AdminUsers",
                columns: new[] { "Id", "CreatedAtUtc", "PasswordHash", "Role", "Username" },
                values: new object[] { 1, new DateTime(2026, 5, 24, 4, 11, 21, 638, DateTimeKind.Utc).AddTicks(8194), "$2a$11$HlDgIuiyVxHd356KWpNR4OtSocmp5RQNYprtQXuKFl6jBlbaRkq3a", 1, "admin" });

            migrationBuilder.CreateIndex(
                name: "IX_ChargeRecords_ContractId",
                table: "ChargeRecords",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractRooms_ContractId",
                table: "ContractRooms",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractRooms_PropertyRoomId",
                table: "ContractRooms",
                column: "PropertyRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_PropertyUnitId",
                table: "Contracts",
                column: "PropertyUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_TenantId",
                table: "Contracts",
                column: "TenantId");

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
                name: "IX_ElectricityBills_ContractId",
                table: "ElectricityBills",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ElectricityMeterReadings_PropertyRoomId_ReadingDateUtc",
                table: "ElectricityMeterReadings",
                columns: new[] { "PropertyRoomId", "ReadingDateUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ElectricityMeterReadings_PropertyUnitId",
                table: "ElectricityMeterReadings",
                column: "PropertyUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRecords_PropertyRoomId",
                table: "ExpenseRecords",
                column: "PropertyRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRecords_PropertyUnitId",
                table: "ExpenseRecords",
                column: "PropertyUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyRooms_PropertyUnitId",
                table: "PropertyRooms",
                column: "PropertyUnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminUsers");

            migrationBuilder.DropTable(
                name: "ChargeRecords");

            migrationBuilder.DropTable(
                name: "ContractRooms");

            migrationBuilder.DropTable(
                name: "ElectricityAllocations");

            migrationBuilder.DropTable(
                name: "ElectricityMeterReadings");

            migrationBuilder.DropTable(
                name: "ExpenseRecords");

            migrationBuilder.DropTable(
                name: "ElectricityBills");

            migrationBuilder.DropTable(
                name: "PropertyRooms");

            migrationBuilder.DropTable(
                name: "Contracts");

            migrationBuilder.DropTable(
                name: "PropertyUnits");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
