using System.Threading.Tasks;
using Api.Requests.Abstractions;
using AspNetCore.ApiControllers.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.ApiControllers.Extensions
{
    public class AsyncApiControllerBaseRequestFor<TResponse>
        where TResponse : IResponse
    {
        private readonly ApiControllerBase _apiController;
        
        public AsyncApiControllerBaseRequestFor(ApiControllerBase apiController)
        {
            _apiController = apiController;
        }
        
        public Task<IActionResult> With<TRequest>(TRequest request)
            where TRequest : IRequest<TResponse>
            => _apiController.RequestAsync<ApiControllerBase, TRequest, TResponse>(request);
    }
}
