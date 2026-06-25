using System.Threading.Tasks;

namespace Api.Requests.Abstractions
{
    public interface IAsyncRequestHandler<in TRequest, TResponse>
        where TRequest : IRequest<TResponse>
        where TResponse : IResponse
    {
        Task<TResponse> ExecuteAsync(TRequest request);
    }
}
