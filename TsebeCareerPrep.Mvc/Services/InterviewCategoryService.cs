
using System.Net.Http.Json;

namespace TsebeCareerPrep.Mvc.Services
{
    public class InterviewCategoryService
    {
        private readonly HttpClient _httpClient;

        public InterviewCategoryService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<InterviewCategoryDto>> GetCategoriesAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<InterviewCategoryDto>>(
                "api/InterviewCategories") ?? new List<InterviewCategoryDto>();
        }
    }

    public class InterviewCategoryDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
