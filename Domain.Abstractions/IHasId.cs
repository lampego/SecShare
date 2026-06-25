using System;

namespace Domain.Abstractions
{
    public interface IHasId
    {
        Guid Id { get; }
    }
}
