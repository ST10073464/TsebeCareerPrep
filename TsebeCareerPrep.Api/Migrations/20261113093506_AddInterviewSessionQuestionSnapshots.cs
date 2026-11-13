using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TsebeCareerPrep.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewSessionQuestionSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InterviewSessionQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InterviewSessionId = table.Column<int>(type: "int", nullable: false),
                    InterviewQuestionId = table.Column<int>(type: "int", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SuggestedAnswer = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DifficultyLevel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewSessionQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewSessionQuestions_InterviewQuestions_InterviewQuestionId",
                        column: x => x.InterviewQuestionId,
                        principalTable: "InterviewQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InterviewSessionQuestions_InterviewSessions_InterviewSessionId",
                        column: x => x.InterviewSessionId,
                        principalTable: "InterviewSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSessionQuestions_InterviewQuestionId",
                table: "InterviewSessionQuestions",
                column: "InterviewQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSessionQuestions_InterviewSessionId_DisplayOrder",
                table: "InterviewSessionQuestions",
                columns: new[] { "InterviewSessionId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSessionQuestions_InterviewSessionId_InterviewQuestionId",
                table: "InterviewSessionQuestions",
                columns: new[] { "InterviewSessionId", "InterviewQuestionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterviewSessionQuestions");
        }
    }
}
