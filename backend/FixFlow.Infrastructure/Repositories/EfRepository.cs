using System.Linq.Expressions;
using FixFlow.Application.Interfaces;
using FixFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Infrastructure.Repositories;

public class EfRepository<T>(FixFlowDbContext dbContext) : IRepository<T> where T : class
{
    public IQueryable<T> Query() => dbContext.Set<T>().AsQueryable();

    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Set<T>().FindAsync([id], cancellationToken).AsTask();

    public Task<T?> FirstAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) =>
        dbContext.Set<T>().FirstOrDefaultAsync(predicate, cancellationToken);

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        await dbContext.Set<T>().AddAsync(entity, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default) =>
        await dbContext.Set<T>().AddRangeAsync(entities, cancellationToken);

    public void Update(T entity) => dbContext.Set<T>().Update(entity);

    public void Remove(T entity) => dbContext.Set<T>().Remove(entity);
}
