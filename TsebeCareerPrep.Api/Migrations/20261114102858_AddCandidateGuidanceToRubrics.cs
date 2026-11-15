using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TsebeCareerPrep.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateGuidanceToRubrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CandidateGuidance",
                table: "InterviewQuestionRubrics",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CandidateGuidance",
                table: "InterviewQuestionRubrics");
        }
    }
}
