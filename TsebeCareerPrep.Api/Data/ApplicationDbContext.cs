
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TsebeCareerPrep.Api.Models;

namespace TsebeCareerPrep.Api.Data
{
    public class ApplicationDbContext : IdentityDbContext
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

        public DbSet<InterviewAssessment> InterviewAssessments
            => Set<InterviewAssessment>();

        public DbSet<InterviewQuestionRubric> InterviewQuestionRubrics
            => Set<InterviewQuestionRubric>();

        // Stores the questions assigned to each interview session.
        // Question content is preserved independently of later edits.
        public DbSet<InterviewSessionQuestion> InterviewSessionQuestions
            => Set<InterviewSessionQuestion>();

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Protect interview session updates from concurrent writes.
            modelBuilder.Entity<InterviewSession>()
                .Property(s => s.RowVersion)
                .IsRowVersion();

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

            // IdentityUser -> InterviewSession
            // Each session belongs to one registered user.
            modelBuilder.Entity<InterviewSession>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .IsRequired()
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

            // InterviewResponse -> InterviewAssessment
            // Each submitted answer can have one assessment.
            modelBuilder.Entity<InterviewAssessment>()
                .HasOne(a => a.InterviewResponse)
                .WithOne()
                .HasForeignKey<InterviewAssessment>(
                    a => a.InterviewResponseId)
                .OnDelete(DeleteBehavior.Cascade);

            // InterviewQuestion -> InterviewQuestionRubric
            // Each interview question can have one marking rubric.
            modelBuilder.Entity<InterviewQuestionRubric>()
                .HasOne(r => r.InterviewQuestion)
                .WithOne()
                .HasForeignKey<InterviewQuestionRubric>(
                    r => r.InterviewQuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            // InterviewSession -> InterviewSessionQuestion
            // Deleting a session removes its assigned snapshots.
            modelBuilder.Entity<InterviewSessionQuestion>()
                .HasOne(sq => sq.InterviewSession)
                .WithMany()
                .HasForeignKey(sq => sq.InterviewSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // InterviewQuestion -> InterviewSessionQuestion
            // Preserve original questions referenced by snapshots.
            modelBuilder.Entity<InterviewSessionQuestion>()
                .HasOne(sq => sq.InterviewQuestion)
                .WithMany()
                .HasForeignKey(sq => sq.InterviewQuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent assigning the same question twice
            // to the same interview session.
            modelBuilder.Entity<InterviewSessionQuestion>()
                .HasIndex(sq => new
                {
                    sq.InterviewSessionId,
                    sq.InterviewQuestionId
                })
                .IsUnique();

            // Preserve a unique display position within each session.
            modelBuilder.Entity<InterviewSessionQuestion>()
                .HasIndex(sq => new
                {
                    sq.InterviewSessionId,
                    sq.DisplayOrder
                })
                .IsUnique();
        }
    }
}
