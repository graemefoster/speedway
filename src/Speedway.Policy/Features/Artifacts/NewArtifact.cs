using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Speedway.Policy.Features.Artifacts
{
    public static class NewArtifact
    {
        public record Request(Guid SpeedwayProjectId, string BuildId, string ArtifactType, object ArtifactContent) : IRequest<Response>;

        public record Response(string ArtifactId);

        public record PolicyEvaluation(bool Satisfied);

        public class Validator : AbstractValidator<Request>
        {
            public Validator()
            {
                RuleFor(x => x.ArtifactType).Must(x => new[] {"RiskDocument", "TestSignOff"}.Contains(x));
            }
        }

        /// <summary>
        /// </summary>
        // ReSharper disable once UnusedType.Local
        class RequestHandler : IRequestHandler<Request, Response>
        {
            private readonly ILogger<RequestHandler> _logger;

            public RequestHandler(ILogger<RequestHandler> logger)
            {
                _logger = logger;
            }

            public Task<Response> Handle(Request request, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Uploaded artifact");
                return Task.FromResult(new Response(Guid.NewGuid().ToString().ToLowerInvariant()));
            }
        }
    }
}