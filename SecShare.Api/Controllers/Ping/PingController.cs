using AspNetCore.ApiControllers.Extensions;
using Autofac;
using Microsoft.AspNetCore.Mvc;
using SecShare.Api.Controllers.Ping.Actions;
using SecShare.Business.Mvc.Controllers;

namespace SecShare.Api.Controllers.Ping;

[ApiController]
[Route("api/[controller]")]
public class PingController(ILifetimeScope scope) : MainApiControllerBase(scope)
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> Get()
        => this.RequestAsync()
            .For<PingResponse>()
            .With(new PingRequest());
}
