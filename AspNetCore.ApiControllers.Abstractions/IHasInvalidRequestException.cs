using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AspNetCore.ApiControllers.Abstractions;

public interface IHasInvalidRequestException
{
    Func<ModelStateDictionary, Exception> InvalidRequestException { get; }
}
