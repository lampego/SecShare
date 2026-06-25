using Autofac;
using Microsoft.AspNetCore.Http;
using Persistence.Transactions.Behaviors;

namespace SecShare.Business.Mvc.Middleware;

public class CommitPerformerMiddleware
{
    private readonly RequestDelegate _next;

    public CommitPerformerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILifetimeScope scope)
    {
        var dbSessionProvider = scope.Resolve<IDbSessionProvider>();

        try
        {
            dbSessionProvider.SetTransactional();
            await _next(context);
            await dbSessionProvider.PerformCommitAsync();
        }
        catch
        {
            await dbSessionProvider.RollbackCommitAsync();
            throw;
        }
        finally
        {
            dbSessionProvider.Dispose();
        }
    }
}
