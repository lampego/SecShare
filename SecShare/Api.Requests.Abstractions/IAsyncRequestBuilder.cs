using System.Threading.Tasks;

namespace Api.Requests.Abstractions
{
    public interface IAsyncRequestBuilder
    {
        Task ExecuteAsync<TRequest>(TRequest request)
            where TRequest : IRequest;

        Task<TRequestResult> ExecuteAsync<TRequest, TRequestResult>(TRequest request)
            where TRequest : IRequest<TRequestResult>
            where TRequestResult : IResponse;
    }
}