using System;
using Autofac;

namespace Api.Requests.Abstractions
{
    public class ScopedAsyncRequestHandlerFactory: IAsyncRequestHandlerFactory
    {
        private readonly ILifetimeScope _scope;

        public ScopedAsyncRequestHandlerFactory(ILifetimeScope scope)
        {
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        }

        public IAsyncRequestHandler<TRequest> Create<TRequest>() where TRequest : IRequest
        {
            return _scope.Resolve<IAsyncRequestHandler<TRequest>>();
        }

        public IAsyncRequestHandler<TRequest, TResponse> Create<TRequest, TResponse>() 
            where TRequest
            : IRequest<TResponse> where TResponse : IResponse
        {
            return _scope.Resolve<IAsyncRequestHandler<TRequest, TResponse>>();
        }
    }
}