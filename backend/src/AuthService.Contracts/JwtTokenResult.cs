namespace AuthService.Contracts;

public sealed record JwtTokenResult(string AccessToken, DateTimeOffset ExpiresAt);

public sealed record JwtLoginRequest(string Email, string Password);

public sealed record JwtLoginResponse(string AccessToken, DateTimeOffset ExpiresAt);