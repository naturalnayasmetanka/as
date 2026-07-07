using System.Net;

namespace ProjectsService.Core.AuthService;

public sealed record AuthCurrentUser(Guid Id, string Email);

public sealed record AuthUser(Guid Id, string Email, string[] Roles);

public sealed record AuthServiceClientError(HttpStatusCode StatusCode, string Message)
{
    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;
    public bool IsForbidden => StatusCode == HttpStatusCode.Forbidden;
}

internal sealed record AuthServiceEnvelope<T>(
    T? Result,
    AuthServiceEnvelopeError? Error,
    bool IsError);

internal sealed record AuthServiceEnvelopeError(AuthServiceEnvelopeMessage[]? Messages);

internal sealed record AuthServiceEnvelopeMessage(string? Message);

internal sealed record AuthGetMeResponse(Guid Id, string Email);

internal sealed record AuthAdminUserResponse(Guid Id, string Email, string[] Roles);
