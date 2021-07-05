using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Speedway.Policy.Features.ExecutableCompliance
{
    [Route("/[controller]")]
    [Authorize(AuthenticationSchemes = "ApiKeyAuthentication")]
    [ApiController]
    public class ComplianceController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ComplianceController> _logger;

        public ComplianceController(IMediator mediator, ILogger<ComplianceController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// TODO - this should be moved to a queue / function to offload to the background. Durable function might be a good approach to it to break it into small chunks.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public Task<EvaluatePolicy.Response> Post(EvaluatePolicy.Request request)
        {
            _logger.LogInformation("We are in the controller!");
            return _mediator.Send(request);
        }
    }
}