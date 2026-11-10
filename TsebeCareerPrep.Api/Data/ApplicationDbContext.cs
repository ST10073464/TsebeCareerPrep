
using Microsoft.EntityFrameworkCore;
using TsebeCareerPrep.Api.Models;

namespace TsebeCareerPrep.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<InterviewCategory> InterviewCategories
            => Set<InterviewCategory>();

        public DbSet<InterviewQuestion> InterviewQuestions
            => Set<InterviewQuestion>();

        public DbSet<InterviewSession> InterviewSessions
            => Set<InterviewSession>();

        public DbSet<InterviewResponse> InterviewResponses
            => Set<InterviewResponse>();

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // InterviewCategory -> InterviewQuestion
            modelBuilder.Entity<InterviewQuestion>()
                .HasOne(q => q.InterviewCategory)
                .WithMany()
                .HasForeignKey(q => q.InterviewCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // InterviewCategory -> InterviewSession
            modelBuilder.Entity<InterviewSession>()
                .HasOne(s => s.InterviewCategory)
                .WithMany()
                .HasForeignKey(s => s.InterviewCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // InterviewSession -> InterviewResponse
            modelBuilder.Entity<InterviewResponse>()
                .HasOne(r => r.InterviewSession)
                .WithMany(s => s.Responses)
                .HasForeignKey(r => r.InterviewSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // InterviewQuestion -> InterviewResponse
            modelBuilder.Entity<InterviewResponse>()
                .HasOne(r => r.InterviewQuestion)
                .WithMany()
                .HasForeignKey(r => r.InterviewQuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
