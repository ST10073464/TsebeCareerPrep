using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TsebeCareerPrep.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewQuestionRubrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InterviewQuestionRubrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InterviewQuestionId = table.Column<int>(type: "int", nullable: false),
                    ExpectedConcepts = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AcceptableAlternatives = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AssessmentGuidance = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TechnicalAccuracyMarks = table.Column<int>(type: "int", nullable: false),
                    KeyConceptMarks = table.Column<int>(type: "int", nullable: false),
                    ClarityMarks = table.Column<int>(type: "int", nullable: false),
                    PracticalExampleMarks = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewQuestionRubrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewQuestionRubrics_InterviewQuestions_InterviewQuestionId",
                        column: x => x.InterviewQuestionId,
                        principalTable: "InterviewQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestionRubrics_InterviewQuestionId",
                table: "InterviewQuestionRubrics",
                column: "InterviewQuestionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterviewQuestionRubrics");
        }
    }
}
