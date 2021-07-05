using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Speedway.Policy.Features.ExecutableCompliance
{
    public static class EvaluatePolicy
    {
        public record Request(Guid SpeedwayProjectId, string BuildId, string Environment) : IRequest<Response>;

        public record Response(Guid SpeedwayProjectId, string BuildId, string Environment,
            PolicyEvaluation PolicyEvaluation);

        public record PolicyEvaluation(bool Satisfied);

        public class Validator : AbstractValidator<Request>
        {
            public Validator()
            {
            }
        }

        /// <summary>
        /// This is where it's at :)
        /// </summary>
        // ReSharper disable once UnusedType.Local
        class PolicyEvaluationRequestHandler : IRequestHandler<Request, Response>
        {
            private readonly ILogger<PolicyEvaluationRequestHandler> _logger;

            public PolicyEvaluationRequestHandler(ILogger<PolicyEvaluationRequestHandler> logger)
            {
                _logger = logger;
            }

            public Task<Response> Handle(Request request, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Evaluating policy for matching ProjectId {Id}, build {BuildId}",
                    request.SpeedwayProjectId, request.BuildId);

                

                return Task.FromResult(
                    new Response(
                        request.SpeedwayProjectId,
                        request.BuildId,
                        request.Environment,
                        new PolicyEvaluation(request.Environment == "Development")));
            }
        }
    }
}