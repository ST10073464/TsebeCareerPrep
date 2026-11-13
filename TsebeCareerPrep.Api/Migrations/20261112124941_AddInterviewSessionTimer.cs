using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TsebeCareerPrep.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewSessionTimer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ActiveDurationSeconds",
                table: "InterviewSessions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "IsTimerRunning",
                table: "InterviewSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastTimerHeartbeatUtc",
                table: "InterviewSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TimerStartedAtUtc",
                table: "InterviewSessions",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveDurationSeconds",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "IsTimerRunning",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "LastTimerHeartbeatUtc",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "TimerStartedAtUtc",
                table: "InterviewSessions");
        }
    }
}
