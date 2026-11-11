
using System.Net;
using System.Net.Http.Json;
using TsebeCareerPrep.Mvc.Models;

namespace TsebeCareerPrep.Mvc.Services
{
    public sealed class ApiLoginResult
    {
        public required LoginResponse User { get; init; }

        public required string AuthenticationCookie { get; init; }
    }

    public class AuthenticationService
    {
        private readonly HttpClient _httpClient;

        public AuthenticationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ApiLoginResult?> LoginAsync(
            LoginViewModel model)
        {
            var request = new
            {
                email = model.Email,
                password = model.Password
            };

            using var response = await _httpClient.PostAsJsonAsync(
                "api/Auth/login",
                request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            var loginResponse = await response.Content
                .ReadFromJsonAsync<LoginResponse>();

            if (loginResponse == null ||
                string.IsNullOrWhiteSpace(loginResponse.UserId) ||
                string.IsNullOrWhiteSpace(loginResponse.Email))
            {
                return null;
            }

            if (!response.Headers.TryGetValues(
                "Set-Cookie",
                out var cookies))
            {
                return null;
            }

            const string cookieName = "TsebeCareerPrep.Auth=";

            var authenticationCookie = cookies
                .Select(cookie => cookie.Split(';', 2)[0].Trim())
                .FirstOrDefault(cookie =>
                    cookie.StartsWith(
                        cookieName,
                        StringComparison.Ordinal));

            if (string.IsNullOrWhiteSpace(authenticationCookie))
            {
                return null;
            }

            return new ApiLoginResult
            {
                User = loginResponse,
                AuthenticationCookie = authenticationCookie
            };
        }
    }
}
