
using System.ComponentModel.DataAnnotations;

namespace TsebeCareerPrep.Mvc.Models
{
    public class InterviewCategoryAdminViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100, MinimumLength = 3)]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category description is required.")]
        [StringLength(500, MinimumLength = 5)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}
