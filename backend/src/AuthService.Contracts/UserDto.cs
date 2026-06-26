namespace AuthService.Contracts;

public sealed record UserDto(Guid Id, Guid OwnerId, string Name, DateTime CreatedAt);

public sealed record CreateUserRequest(string Email, string Password);

public sealed record CreateUserResponse(Guid UserId, string Token);

public sealed record GetMeResponse(Guid Id, string Email);
