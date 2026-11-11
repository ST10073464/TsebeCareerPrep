using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TsebeCareerPrep.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewAssessments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InterviewAssessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InterviewResponseId = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Strengths = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AreasForImprovement = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ImprovementGuidance = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AssessmentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssessedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewAssessments_InterviewResponses_InterviewResponseId",
                        column: x => x.InterviewResponseId,
                        principalTable: "InterviewResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewAssessments_InterviewResponseId",
                table: "InterviewAssessments",
                column: "InterviewResponseId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterviewAssessments");
        }
    }
}
