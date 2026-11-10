
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

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<InterviewQuestion>()
                .HasOne(q => q.InterviewCategory)
                .WithMany()
                .HasForeignKey(q => q.InterviewCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
