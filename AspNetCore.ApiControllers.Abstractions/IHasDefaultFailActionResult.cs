using System;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.ApiControllers.Abstractions
{
    public interface IHasDefaultFailActionResult
    {
        Func<Exception, IActionResult> Fail { get; }
    }
}