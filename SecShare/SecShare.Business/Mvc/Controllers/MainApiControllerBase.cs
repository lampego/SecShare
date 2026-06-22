using Api.Requests.Abstractions;
using AspNetCore.ApiControllers.Abstractions;
using Autofac;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SecShare.Business.Dto;

namespace SecShare.Business.Mvc.Controllers;

public class MainApiControllerBase : ApiControllerBase
{
    protected readonly ILogger<MainApiControllerBase> Logger;

    public MainApiControllerBase(ILifetimeScope scope)
        : base(scope.Resolve<IAsyncRequestBuilder>())
    {
        Logger = scope.Resolve<ILogger<MainApiControllerBase>>();
    }

    public override Func<IActionResult> Success => () =>
        new OkObjectResult(new JsonCommonResponse());
}
