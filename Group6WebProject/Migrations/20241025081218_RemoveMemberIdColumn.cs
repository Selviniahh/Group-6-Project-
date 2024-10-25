using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Group6WebProject.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMemberIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MemberPreferences_Users_UserID",
                table: "MemberPreferences");

            migrationBuilder.DropIndex(
                name: "IX_MemberPreferences_UserID",
                table: "MemberPreferences");

            migrationBuilder.DropColumn(
                name: "UserID",
                table: "MemberPreferences");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "MemberPreferences",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_MemberPreferences_UserId",
                table: "MemberPreferences",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_MemberPreferences_Users_UserId",
                table: "MemberPreferences",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MemberPreferences_Users_UserId",
                table: "MemberPreferences");

            migrationBuilder.DropIndex(
                name: "IX_MemberPreferences_UserId",
                table: "MemberPreferences");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "MemberPreferences",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "UserID",
                table: "MemberPreferences",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberPreferences_UserID",
                table: "MemberPreferences",
                column: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_MemberPreferences_Users_UserID",
                table: "MemberPreferences",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "UserID");
        }
    }
}
