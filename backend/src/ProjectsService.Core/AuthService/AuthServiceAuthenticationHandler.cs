using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace ProjectsService.Core.AuthService;

public sealed class AuthServiceAuthenticationHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceTokenProvider _serviceTokenProvider;

    public AuthServiceAuthenticationHandler(
        IHttpContextAccessor httpContextAccessor,
        IServiceTokenProvider serviceTokenProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceTokenProvider = serviceTokenProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var inboundAuthorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();

        if (!string.IsNullOrWhiteSpace(inboundAuthorization))
        {
            request.Headers.TryAddWithoutValidation("Authorization", inboundAuthorization);
        }
        else
        {
            var serviceToken = await _serviceTokenProvider.GetTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
