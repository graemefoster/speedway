using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Speedway.Api.Extensions
{
    public class SpeedwayExceptionFilter : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext actionExecutedContext)
        {
            var logger = actionExecutedContext.HttpContext.RequestServices
                .GetRequiredService<ILogger<SpeedwayExceptionFilter>>();
            logger.LogError(actionExecutedContext.Exception, "An error occurred");

            if (actionExecutedContext.Exception is ArgumentException argEx)
            {
                actionExecutedContext.Result = new BadRequestObjectResult(new
                {
                    error = argEx.Message,
                    data = new
                    {
                        information = argEx.Data
                    }
                });
                actionExecutedContext.ExceptionHandled = true;
                return;
            }

            if (actionExecutedContext.Exception is FluentValidation.ValidationException argVex)
            {
                actionExecutedContext.Result = new BadRequestObjectResult(new
                {
                    error = argVex.Message,
                    data = new
                    {
                        information = argVex.Data
                    }
                });
                actionExecutedContext.ExceptionHandled = true;
                return;
            }

            actionExecutedContext.Result = new ObjectResult(new
            {
                error = "An unhandled exception occurred",
                data = new
                {
                    information = actionExecutedContext.Exception.Message,
                    stackTrace = actionExecutedContext.Exception.ToString()
                }
            })
            {
                StatusCode = 500
            };
            actionExecutedContext.ExceptionHandled = true;
        }
    }
}