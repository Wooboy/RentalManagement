using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class ContractMultiRooms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_PropertyRooms_PropertyRoomId",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_PropertyRoomId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "PropertyRoomId",
                table: "Contracts");

            migrationBuilder.CreateTable(
                name: "ContractRooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContractId = table.Column<int>(type: "INTEGER", nullable: false),
                    PropertyRoomId = table.Column<int>(type: "INTEGER", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_ContractRooms_ContractId",
                table: "ContractRooms",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractRooms_PropertyRoomId",
                table: "ContractRooms",
                column: "PropertyRoomId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractRooms");

            migrationBuilder.AddColumn<int>(
                name: "PropertyRoomId",
                table: "Contracts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_PropertyRoomId",
                table: "Contracts",
                column: "PropertyRoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_PropertyRooms_PropertyRoomId",
                table: "Contracts",
                column: "PropertyRoomId",
                principalTable: "PropertyRooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
