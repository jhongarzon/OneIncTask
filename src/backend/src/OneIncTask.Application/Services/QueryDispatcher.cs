using OneIncTask.Domain.Abstractions;

namespace OneIncTask.Application.Services;

public class QueryDispatcher(IServiceProvider serviceProvider) : IQueryDispatcher
{
    public async Task<TResult?> QueryAsync<TResult>(IQuery<TResult> query,
        CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
        var handler = serviceProvider.GetService(handlerType) ??
            throw new InvalidOperationException($"No handler found for query {query.GetType()}");

        return await (Task<TResult?>)handlerType
            .GetMethod("Handle")!
            .Invoke(handler, [query, cancellationToken])!;
    }
}
