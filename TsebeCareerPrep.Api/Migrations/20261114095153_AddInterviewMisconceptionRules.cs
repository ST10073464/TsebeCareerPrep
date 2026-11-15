using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TsebeCareerPrep.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewMisconceptionRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InterviewMisconceptionRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InterviewQuestionId = table.Column<int>(type: "int", nullable: false),
                    TriggerPhrase = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FeedbackMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewMisconceptionRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewMisconceptionRules_InterviewQuestions_InterviewQuestionId",
                        column: x => x.InterviewQuestionId,
                        principalTable: "InterviewQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewMisconceptionRules_InterviewQuestionId_IsActive",
                table: "InterviewMisconceptionRules",
                columns: new[] { "InterviewQuestionId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterviewMisconceptionRules");
        }
    }
}
