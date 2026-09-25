using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace FixFlow.Application.Common;

public static class QueryableMaterializer
{
    public static Task<List<T>> ToMaterializedListAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) =>
        source.Provider is IAsyncQueryProvider
            ? source.ToListAsync(cancellationToken)
            : Task.FromResult(source.ToList());

    public static async Task<T?> FirstMaterializedAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default)
    {
        if (source.Provider is IAsyncQueryProvider)
        {
            return await source.FirstOrDefaultAsync(cancellationToken);
        }

        return source.FirstOrDefault();
    }

    public static Task<bool> AnyMaterializedAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) =>
        source.Provider is IAsyncQueryProvider
            ? source.AnyAsync(cancellationToken)
            : Task.FromResult(source.Any());

    public static Task<int> CountMaterializedAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) =>
        source.Provider is IAsyncQueryProvider
            ? source.CountAsync(cancellationToken)
            : Task.FromResult(source.Count());
}
