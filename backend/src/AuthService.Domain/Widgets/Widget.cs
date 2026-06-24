namespace AuthService.Domain.Widgets;

public sealed class Widget
{
    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public WidgetName Name { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Widget() { }

    private Widget(Guid id, Guid ownerId, WidgetName name)
    {
        Id = id;
        OwnerId = ownerId;
        Name = name;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static Result<Widget, Error> Create(Guid ownerId, WidgetName name)
    {
        var widget = new Widget(Guid.CreateVersion7(), ownerId, name);
        return widget;
    }
}
