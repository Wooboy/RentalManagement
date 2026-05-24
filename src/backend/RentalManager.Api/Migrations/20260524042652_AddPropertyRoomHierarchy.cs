using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyRoomHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PropertyRoomId",
                table: "Contracts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PropertyRooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PropertyUnitId = table.Column<int>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_PropertyRoomId",
                table: "Contracts",
                column: "PropertyRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyRooms_PropertyUnitId",
                table: "PropertyRooms",
                column: "PropertyUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_PropertyRooms_PropertyRoomId",
                table: "Contracts",
                column: "PropertyRoomId",
                principalTable: "PropertyRooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_PropertyRooms_PropertyRoomId",
                table: "Contracts");

            migrationBuilder.DropTable(
                name: "PropertyRooms");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_PropertyRoomId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "PropertyRoomId",
                table: "Contracts");
        }
    }
}
