using ProjectsService.Domain.Projects;

namespace ProjectsService.Core.Database.Abstractions;

public interface IProjectRepository
{
    Task<IReadOnlyCollection<Project>> GetForUserAsync(Guid userId, bool includeAll, CancellationToken cancellationToken);
    Task<Project?> GetByIdAsync(Guid projectId, Guid userId, bool includeAll, CancellationToken cancellationToken);
    Task AddAsync(Project project, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    void Delete(Project project);
}
