
using Microsoft.EntityFrameworkCore;
using TsebeCareerPrep.Api.Models;

namespace TsebeCareerPrep.Api.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(
            ApplicationDbContext context)
        {
            var categories = new[]
            {
                new InterviewCategory
                {
                    Name = "Software Development",
                    Description = "Technical and programming interviews",
                    IsActive = true
                },
                new InterviewCategory
                {
                    Name = "Administration",
                    Description = "Office administration and support interviews",
                    IsActive = true
                },
                new InterviewCategory
                {
                    Name = "Finance",
                    Description = "Accounting and financial management interviews",
                    IsActive = true
                },
                new InterviewCategory
                {
                    Name = "Management",
                    Description = "Leadership and management interviews",
                    IsActive = true
                }
            };

            foreach (var category in categories)
            {
                bool exists = await context.InterviewCategories
                    .AnyAsync(c => c.Name == category.Name);

                if (!exists)
                {
                    context.InterviewCategories.Add(category);
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
