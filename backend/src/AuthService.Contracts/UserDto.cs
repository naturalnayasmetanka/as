namespace AuthService.Contracts;

public sealed record UserDto(Guid Id, Guid OwnerId, string Name, DateTime CreatedAt);

public sealed record CreateUserRequest(string Email, string Password);

public sealed record CreateUserResponse(Guid UserId);

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(Guid UserId, string Email);

public sealed record GetMeResponse(Guid Id, string Email, IReadOnlyCollection<string> Roles);

public sealed record AdminUserDto(Guid Id, string Email, IReadOnlyCollection<string> Roles);
