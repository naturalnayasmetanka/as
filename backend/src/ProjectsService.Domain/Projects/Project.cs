namespace ProjectsService.Domain.Projects;

public sealed class Project
{
    private Project()
    {
    }

    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ProjectStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Project Create(Guid ownerId, string name, string description)
    {
        var now = DateTimeOffset.UtcNow;

        return new Project
        {
            Id = Guid.CreateVersion7(),
            OwnerId = ownerId,
            Name = name.Trim(),
            Description = description.Trim(),
            Status = ProjectStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string name, string description, ProjectStatus status)
    {
        Name = name.Trim();
        Description = description.Trim();
        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
