
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TsebeCareerPrep.Api.Migrations
{
    public partial class AddInterviewSessionUserOwnership : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add the UserId column.
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "InterviewSessions",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            // Step 2: Assign existing test sessions
            // to the registered test account.
            // Fail safely if the account is missing.
            migrationBuilder.Sql(@"
                DECLARE @TestUserId nvarchar(450);

                SELECT @TestUserId = Id
                FROM dbo.AspNetUsers
                WHERE NormalizedEmail = N'TESTUSER1@EXAMPLE.COM';

                IF @TestUserId IS NULL
                BEGIN
                    THROW 50001,
                        'Test user not found. Cannot migrate legacy sessions.',
                        1;
                END;

                UPDATE dbo.InterviewSessions
                SET UserId = @TestUserId
                WHERE UserId = N'';
            ");

            // Step 3: Create the UserId index.
            migrationBuilder.CreateIndex(
                name: "IX_InterviewSessions_UserId",
                table: "InterviewSessions",
                column: "UserId");

            // Step 4: Enforce the Identity user relationship.
            migrationBuilder.AddForeignKey(
                name: "FK_InterviewSessions_AspNetUsers_UserId",
                table: "InterviewSessions",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterviewSessions_AspNetUsers_UserId",
                table: "InterviewSessions");

            migrationBuilder.DropIndex(
                name: "IX_InterviewSessions_UserId",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "InterviewSessions");
        }
    }
}
