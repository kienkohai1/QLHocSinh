using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLHocSinh.Migrations
{
    /// <inheritdoc />
    public partial class AddAcademicYearSemesterToGrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcademicYear",
                table: "Grades",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Semester",
                table: "Grades",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcademicYear",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "Semester",
                table: "Grades");
        }
    }
}
