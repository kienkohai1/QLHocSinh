using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLHocSinh.Migrations
{
    /// <inheritdoc />
    public partial class MakeUserFieldsNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_AspNetUsers_ParentId",
                table: "Students");

            migrationBuilder.AlterColumn<string>(
                name: "ParentId",
                table: "Students",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_AspNetUsers_ParentId",
                table: "Students",
                column: "ParentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_AspNetUsers_ParentId",
                table: "Students");

            migrationBuilder.AlterColumn<string>(
                name: "ParentId",
                table: "Students",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_AspNetUsers_ParentId",
                table: "Students",
                column: "ParentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
