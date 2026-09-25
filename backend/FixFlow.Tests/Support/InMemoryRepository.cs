using System.Linq.Expressions;
using System.Reflection;
using FixFlow.Application.Interfaces;

namespace FixFlow.Tests.Support;

public class InMemoryRepository<T> : IRepository<T> where T : class
{
    public List<T> Items { get; } = [];

    public IQueryable<T> Query() => Items.AsQueryable();

    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var property = typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        var match = Items.FirstOrDefault(item => property is not null && (Guid)property.GetValue(item)! == id);
        return Task.FromResult(match);
    }

    public Task<T?> FirstAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.AsQueryable().FirstOrDefault(predicate));

    public Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        Items.Add(entity);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        Items.AddRange(entities);
        return Task.CompletedTask;
    }

    public void Update(T entity)
    {
    }

    public void Remove(T entity) => Items.Remove(entity);
}

public class InMemoryUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default) =>
        await action();

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default) =>
        await action();
}

public class TestCurrentUser : ICurrentUser
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = "customer@fixflow.test";
    public Domain.Enums.UserRole Role { get; set; } = Domain.Enums.UserRole.Customer;
    public bool IsAuthenticated => true;
    public bool IsAdmin => Role == Domain.Enums.UserRole.Admin;
    public bool IsCustomer => Role == Domain.Enums.UserRole.Customer;
    public bool IsTechnician => Role == Domain.Enums.UserRole.Technician;
}
