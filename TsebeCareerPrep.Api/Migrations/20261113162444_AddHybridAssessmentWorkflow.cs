using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TsebeCareerPrep.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddHybridAssessmentWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssessmentStatus",
                table: "InterviewAssessments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ProvisionalScore",
                table: "InterviewAssessments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "InterviewAssessments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedByUserId",
                table: "InterviewAssessments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssessmentStatus",
                table: "InterviewAssessments");

            migrationBuilder.DropColumn(
                name: "ProvisionalScore",
                table: "InterviewAssessments");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "InterviewAssessments");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "InterviewAssessments");
        }
    }
}
