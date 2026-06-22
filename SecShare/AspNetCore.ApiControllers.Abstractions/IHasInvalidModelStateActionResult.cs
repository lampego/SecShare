using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AspNetCore.ApiControllers.Abstractions
{
    public interface IHasInvalidModelStateActionResult
    {
        Func<ModelStateDictionary, IActionResult> InvalidModelState { get; }
    }
}