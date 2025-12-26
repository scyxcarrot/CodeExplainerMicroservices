using Microsoft.AspNetCore.Authentication;

namespace ChatService.DelegatingHandlers
{
    public class AuthorizationDelegatingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Get the access token from the current request's HttpContext.
            var accessToken = await httpContextAccessor.HttpContext.GetTokenAsync("access_token");

            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
