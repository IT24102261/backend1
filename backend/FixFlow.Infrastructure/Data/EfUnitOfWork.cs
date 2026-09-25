using FixFlow.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Infrastructure.Data;

public class EfUnitOfWork(FixFlowDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    public Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default) =>
        ExecuteInTransactionAsync(async () =>
        {
            await action();
            return true;
        }, cancellationToken);

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var result = await action();
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }
}
