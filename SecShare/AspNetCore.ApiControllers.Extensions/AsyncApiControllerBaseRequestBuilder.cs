using Api.Requests.Abstractions;
using AspNetCore.ApiControllers.Abstractions;

namespace AspNetCore.ApiControllers.Extensions
{
    public class AsyncApiControllerBaseRequestBuilder
    {
        private readonly ApiControllerBase _apiController;

        public AsyncApiControllerBaseRequestBuilder(ApiControllerBase apiController)
        {
            _apiController = apiController;
        }

        public AsyncApiControllerBaseRequestFor<TResponse> For<TResponse>() 
            where TResponse : IResponse 
            => new (_apiController);
    }
}