using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Shared.Authorization;

public sealed class PermissionAuthorizationOptions
{
    public string AuthenticationScheme { get; init; } = JwtBearerDefaults.AuthenticationScheme;
}
