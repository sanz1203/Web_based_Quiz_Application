using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quiz_Application.Migrations
{
    /// <inheritdoc />
    public partial class AddMethodologyToQuiz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MethodologyFileName",
                table: "Quizzes",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MethodologyFileName",
                table: "Quizzes");
        }
    }
}
