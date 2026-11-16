
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TsebeCareerPrep.Api.Migrations
{
    public partial class AddInterviewSessionUserOwnership : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ======================================================
            // STEP 1: ADD USER OWNERSHIP COLUMN
            // ======================================================

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "InterviewSessions",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");


            // ======================================================
            // STEP 2: HANDLE EXISTING INTERVIEW SESSIONS
            // ======================================================

            // New Azure databases may contain no interview sessions.
            // Only require the legacy test account when there are
            // existing sessions that need ownership assigned.

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1
                    FROM dbo.InterviewSessions
                    WHERE UserId = N''
                )
                BEGIN
                    DECLARE @TestUserId nvarchar(450);

                    SELECT TOP (1)
                        @TestUserId = Id
                    FROM dbo.AspNetUsers
                    WHERE NormalizedEmail =
                        N'TESTUSER1@EXAMPLE.COM';

                    IF @TestUserId IS NULL
                    BEGIN
                        THROW 50001,
                            'Test user not found. Cannot migrate legacy sessions.',
                            1;
                    END;

                    UPDATE dbo.InterviewSessions
                    SET UserId = @TestUserId
                    WHERE UserId = N'';
                END;
            ");


            // ======================================================
            // STEP 3: CREATE USER OWNERSHIP INDEX
            // ======================================================

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSessions_UserId",
                table: "InterviewSessions",
                column: "UserId");


            // ======================================================
            // STEP 4: ENFORCE IDENTITY USER RELATIONSHIP
            // ======================================================

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
            // ======================================================
            // ROLLBACK USER OWNERSHIP
            // ======================================================

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
