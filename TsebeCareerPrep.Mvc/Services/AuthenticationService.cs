
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

            using var response =
                await _httpClient.PostAsJsonAsync(
                    "api/Auth/login",
                    request);

            if (response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.BadRequest)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var loginResponse = await response.Content
                .ReadFromJsonAsync<LoginResponse>();

            if (loginResponse == null ||
                string.IsNullOrWhiteSpace(loginResponse.UserId) ||
                string.IsNullOrWhiteSpace(loginResponse.Email))
            {
                throw new InvalidOperationException(
                    "The API returned an incomplete login response.");
            }

            if (!response.Headers.TryGetValues(
                "Set-Cookie",
                out var cookies))
            {
                throw new InvalidOperationException(
                    "The API did not return an authentication cookie.");
            }

            const string cookieName = "TsebeCareerPrep.Auth=";

            var authenticationCookie = cookies
                .Select(cookie =>
                    cookie.Split(';', 2)[0].Trim())
                .FirstOrDefault(cookie =>
                    cookie.StartsWith(
                        cookieName,
                        StringComparison.Ordinal));

            if (string.IsNullOrWhiteSpace(authenticationCookie))
            {
                throw new InvalidOperationException(
                    "The API authentication cookie was missing.");
            }

            return new ApiLoginResult
            {
                User = loginResponse,
                AuthenticationCookie = authenticationCookie
            };
        }


        public async Task<(bool Success, string Message)> RegisterAsync(
            RegisterViewModel model)
        {
            var request = new
            {
                email = model.Email.Trim(),
                password = model.Password,
                confirmPassword = model.ConfirmPassword
            };

            using var response = await _httpClient.PostAsJsonAsync(
                "api/Auth/register",
                request);

            if (response.IsSuccessStatusCode)
            {
                return (
                    true,
                    "Account created successfully. Please sign in."
                );
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return (
                    false,
                    "An account with this email address already exists."
                );
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                using var document = await System.Text.Json.JsonDocument
                    .ParseAsync(await response.Content.ReadAsStreamAsync());

                var root = document.RootElement;

                if (root.TryGetProperty("errors", out var errors) &&
                    errors.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var messages = errors.EnumerateArray()
                        .Select(error => error.GetString())
                        .Where(message => !string.IsNullOrWhiteSpace(message));

                    return (
                        false,
                        string.Join(" ", messages)
                    );
                }

                return (
                    false,
                    "Please check your registration details and try again."
                );
            }

            // Unexpected API failures should not be presented
            // to the candidate as successful registrations.
            response.EnsureSuccessStatusCode();

            return (
                false,
                "Registration could not be completed."
            );
        }

    }
}
