using Api.Requests.Abstractions;

namespace AspNetCore.ApiControllers.Abstractions
{
    public interface IAsyncApiController
    {
        IAsyncRequestBuilder AsyncRequestBuilder { get; }
    }
}