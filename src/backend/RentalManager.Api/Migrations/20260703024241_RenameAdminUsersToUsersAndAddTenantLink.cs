using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalManager.Api.Migrations
{
    /// <summary>
    /// AdminUsers 更名為 Users 並加入租客帳號欄位（DisplayName/Email/TenantId/IsActive）。
    /// 手動改寫為 RenameTable + AddColumn 以保留既有帳號資料。
    /// </summary>
    public partial class RenameAdminUsersToUsersAndAddTenantLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "AdminUsers",
                newName: "Users");

            migrationBuilder.Sql("""ALTER TABLE "Users" RENAME CONSTRAINT "PK_AdminUsers" TO "PK_Users";""");
            migrationBuilder.Sql("""ALTER SEQUENCE IF EXISTS "AdminUsers_Id_seq" RENAME TO "Users_Id_seq";""");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId",
                table: "Users",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "Users");

            migrationBuilder.Sql("""ALTER SEQUENCE IF EXISTS "Users_Id_seq" RENAME TO "AdminUsers_Id_seq";""");
            migrationBuilder.Sql("""ALTER TABLE "Users" RENAME CONSTRAINT "PK_Users" TO "PK_AdminUsers";""");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "AdminUsers");
        }
    }
}
