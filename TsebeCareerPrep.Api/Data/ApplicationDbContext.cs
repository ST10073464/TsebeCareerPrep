
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
    }
}

