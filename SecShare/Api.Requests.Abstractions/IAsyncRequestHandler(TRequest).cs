using System.Threading.Tasks;

namespace Api.Requests.Abstractions
{
    public interface IAsyncRequestHandler<in TRequest>
        where TRequest : IRequest
    {
        Task ExecuteAsync(TRequest request);
    }
}