using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitWithPropertyUnitRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    BillingStartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BillingEndUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PropertyUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyUnits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    TaxId = table.Column<string>(type: "TEXT", nullable: true),
                    PersonalId = table.Column<string>(type: "TEXT", nullable: true),
                    Phone = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    EmergencyContactName = table.Column<string>(type: "TEXT", nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContractNo = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    PropertyUnitId = table.Column<int>(type: "INTEGER", nullable: true),
                    PropertyName = table.Column<string>(type: "TEXT", nullable: false),
                    PropertyAddress = table.Column<string>(type: "TEXT", nullable: false),
                    StartDateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MonthlyRent = table.Column<decimal>(type: "TEXT", nullable: false),
                    Deposit = table.Column<decimal>(type: "TEXT", nullable: false),
                    OccupantCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ElectricityRuleType = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                name: "ChargeRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContractId = table.Column<int>(type: "INTEGER", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    BillingStartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BillingEndUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MeterStart = table.Column<decimal>(type: "TEXT", nullable: true),
                    MeterEnd = table.Column<decimal>(type: "TEXT", nullable: true),
                    UsageUnits = table.Column<decimal>(type: "TEXT", nullable: true),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    IsPaid = table.Column<bool>(type: "INTEGER", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
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

            migrationBuilder.InsertData(
                table: "AdminUsers",
                columns: new[] { "Id", "CreatedAtUtc", "PasswordHash", "Username" },
                values: new object[] { 1, new DateTime(2026, 5, 24, 3, 45, 34, 225, DateTimeKind.Utc).AddTicks(4269), "$2a$11$MwEuGkUTNc2sU52LKaYQvuSM/UMIOb5Ue2xju9IHNLgIbP.ygHpWe", "admin" });

            migrationBuilder.CreateIndex(
                name: "IX_ChargeRecords_ContractId",
                table: "ChargeRecords",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_PropertyUnitId",
                table: "Contracts",
                column: "PropertyUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_TenantId",
                table: "Contracts",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminUsers");

            migrationBuilder.DropTable(
                name: "ChargeRecords");

            migrationBuilder.DropTable(
                name: "ExpenseRecords");

            migrationBuilder.DropTable(
                name: "Contracts");

            migrationBuilder.DropTable(
                name: "PropertyUnits");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
