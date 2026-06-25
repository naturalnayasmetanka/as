using AuthService.Core.Database;
using System.Data.Common;

namespace AuthService.Infrastructure.Postgres;

internal sealed class TransactionManager : ITransactionManager
{
    private readonly AuthServiceDbContext _dbContext;

    public TransactionManager(AuthServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public DbConnection GetDbConnection() => _dbContext.Database.GetDbConnection();

    public async Task<Result<T, Error>> CommitTransactionAsync<T>(
        Func<CancellationToken, Task<Result<T, Error>>> work,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            Result<T, Error> result = await work(cancellationToken);
            if (result.IsFailure)
            {
                await transaction.RollbackAsync(cancellationToken);
                return result;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
