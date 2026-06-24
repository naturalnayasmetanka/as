using System;

namespace AuthService.Contracts;

public sealed record WidgetDto(Guid Id, Guid OwnerId, string Name, DateTime CreatedAt);

public sealed record CreateWidgetRequest(string Name);
