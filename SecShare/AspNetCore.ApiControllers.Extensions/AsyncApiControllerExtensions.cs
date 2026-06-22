using System;
using System.Threading.Tasks;
using Api.Requests.Abstractions;
using AspNetCore.ApiControllers.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.ApiControllers.Extensions
{
    public static class AsyncApiControllerExtensions
    {
        public static Task<IActionResult> RequestAsync<TApiController, TRequest>(
            this TApiController apiController,
            TRequest request
        )
            where TApiController : 
                ControllerBase, 
                IAsyncApiController, 
                IHasDefaultSuccessActionResult,
                IHasInvalidModelStateActionResult
            where TRequest : IRequest
            => RequestAsync(
                apiController,
                request,
                apiController.Success
            );

        public static async Task<IActionResult> RequestAsync<TApiController, TRequest>(
            this TApiController apiController,
            TRequest request,
            Func<IActionResult> success
        )
            where TApiController : 
                ControllerBase,
                IAsyncApiController, 
                IHasInvalidModelStateActionResult
            where TRequest : IRequest
        {
            if (apiController == null)
                throw new ArgumentNullException(nameof(apiController));

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!apiController.ModelState.IsValid)
                return apiController.InvalidModelState(apiController.ModelState);
                
            await apiController.AsyncRequestBuilder.ExecuteAsync(request);
            return success();
        }
        
        public static Task<IActionResult> RequestAsync<TApiController, TRequest, TResponse>(
            this TApiController apiController,
            TRequest request)
            where TApiController : 
                ControllerBase, 
                IAsyncApiController, 
                IHasDefaultResponseSuccessActionResult,
                IHasInvalidModelStateActionResult
            where TRequest : IRequest<TResponse>
            where TResponse : IResponse
            => RequestAsync(
                apiController,
                request,
                apiController.ResponseSuccess<TResponse>()
            );

        public static async Task<IActionResult> RequestAsync<TApiController, TRequest, TResponse>(
            this TApiController apiController,
            TRequest request,
            Func<TResponse, IActionResult> success
        )
            where TApiController : 
                ControllerBase, 
                IAsyncApiController, 
                IHasInvalidModelStateActionResult
            where TRequest : IRequest<TResponse>
            where TResponse : IResponse
        {
            if (apiController == null)
                throw new ArgumentNullException(nameof(apiController));

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!apiController.ModelState.IsValid)
                return apiController.InvalidModelState(apiController.ModelState);

            var response = await apiController.AsyncRequestBuilder.ExecuteAsync<TRequest, TResponse>(request);
            if (response is FileResponse or FileResult)
            {
                return response as IActionResult;
            }

            return success(response);
        }
    }
}
