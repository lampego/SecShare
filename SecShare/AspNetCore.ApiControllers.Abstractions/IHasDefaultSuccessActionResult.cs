using System;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.ApiControllers.Abstractions
{
    public interface IHasDefaultSuccessActionResult
    {
        Func<IActionResult> Success { get; }
    }
}