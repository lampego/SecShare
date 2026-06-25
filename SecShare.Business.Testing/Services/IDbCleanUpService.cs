using Domain.Abstractions;

namespace SecShare.Business.Testing.Services;

public interface IDbCleanUpService : IDomainService
{
    Task CleanUp();
}
