using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RentalManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRepairTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RepairTickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContractId = table.Column<int>(type: "integer", nullable: false),
                    PropertyUnitId = table.Column<int>(type: "integer", nullable: true),
                    PropertyRoomId = table.Column<int>(type: "integer", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    HandledByUserId = table.Column<int>(type: "integer", nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairTickets_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RepairTickets_PropertyRooms_PropertyRoomId",
                        column: x => x.PropertyRoomId,
                        principalTable: "PropertyRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RepairTickets_PropertyUnits_PropertyUnitId",
                        column: x => x.PropertyUnitId,
                        principalTable: "PropertyUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RepairTickets_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RepairTickets_Users_HandledByUserId",
                        column: x => x.HandledByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RepairTicketComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RepairTicketId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairTicketComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairTicketComments_RepairTickets_RepairTicketId",
                        column: x => x.RepairTicketId,
                        principalTable: "RepairTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RepairTicketComments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RepairTicketComments_RepairTicketId",
                table: "RepairTicketComments",
                column: "RepairTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTicketComments_UserId",
                table: "RepairTicketComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTickets_ContractId",
                table: "RepairTickets",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTickets_CreatedByUserId",
                table: "RepairTickets",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTickets_HandledByUserId",
                table: "RepairTickets",
                column: "HandledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTickets_PropertyRoomId",
                table: "RepairTickets",
                column: "PropertyRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTickets_PropertyUnitId",
                table: "RepairTickets",
                column: "PropertyUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTickets_Status",
                table: "RepairTickets",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RepairTicketComments");

            migrationBuilder.DropTable(
                name: "RepairTickets");
        }
    }
}
