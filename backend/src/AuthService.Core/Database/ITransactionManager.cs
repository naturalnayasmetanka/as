namespace AuthService.Core.Database;

using System.Data.Common;

public interface ITransactionManager
{
    DbConnection GetDbConnection();

    Task<Result<T, Error>> CommitTransactionAsync<T>(
        Func<CancellationToken, Task<Result<T, Error>>> work,
        CancellationToken cancellationToken = default);
}
