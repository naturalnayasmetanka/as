using ProjectsService.Core.Database.Abstractions;
using ProjectsService.Domain.Projects;

namespace ProjectsService.Infrastructure.Postgres.Repositories;

public sealed class ProjectRepository : IProjectRepository
{
    private readonly ProjectsServiceDbContext _dbContext;

    public ProjectRepository(ProjectsServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Project>> GetForUserAsync(
        Guid userId,
        bool includeAll,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Projects.AsNoTracking();

        if (!includeAll)
        {
            query = query.Where(p => p.OwnerId == userId);
        }

        return await query
            .OrderByDescending(p => p.UpdatedAt)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<Project?> GetByIdAsync(
        Guid projectId,
        Guid userId,
        bool includeAll,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Projects.AsQueryable();

        if (!includeAll)
        {
            query = query.Where(p => p.OwnerId == userId);
        }

        return await query.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
    }

    public async Task AddAsync(Project project, CancellationToken cancellationToken) =>
        await _dbContext.Projects.AddAsync(project, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    public void Delete(Project project) =>
        _dbContext.Projects.Remove(project);
}
