using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Speedway.PipelineBuilderApi.Features.Containers;

namespace Speedway.PipelineBuilderApi
{
    // ReSharper disable once UnusedType.Global
    /// <summary>
    /// Attached Http request information to the current request context
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    internal class UserNamePipelineDecorator<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICurrentUserContext _currentUserContext;

        public UserNamePipelineDecorator(IHttpContextAccessor httpContextAccessor, ICurrentUserContext currentUserContext)
        {
            _httpContextAccessor = httpContextAccessor;
            _currentUserContext = currentUserContext;
        }
        
        public Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            if (_httpContextAccessor.HttpContext == null) throw new InvalidOperationException("Attempt to access HttpContext outside of a Http Call");
            _currentUserContext.UserName = _httpContextAccessor.HttpContext.User.Claims.First(x => x.Type == ClaimTypes.Email).Value;
            return next();
        }
    }
}