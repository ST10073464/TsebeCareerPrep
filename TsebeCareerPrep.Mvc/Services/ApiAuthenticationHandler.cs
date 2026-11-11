
using Microsoft.AspNetCore.Http;

namespace TsebeCareerPrep.Mvc.Services
{
    public sealed class ApiAuthenticationHandler
        : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiAuthenticationHandler(
            IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var context = _httpContextAccessor.HttpContext;

            if (context?.User.Identity?.IsAuthenticated == true &&
                context.Request.Cookies.TryGetValue(
                    "TsebeCareerPrep.Auth",
                    out var authenticationTicket) &&
                !string.IsNullOrWhiteSpace(authenticationTicket))
            {
                request.Headers.TryAddWithoutValidation(
                    "Cookie",
                    $"TsebeCareerPrep.Auth={authenticationTicket}");
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
