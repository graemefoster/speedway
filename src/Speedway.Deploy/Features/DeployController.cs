using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Speedway.Deploy.Features
{
    [Route("/[controller]")]
    [Authorize(Roles = "access_as_application")]
    [ApiController]
    public class DeployController : ControllerBase  
    {
        private readonly IMediator _mediator;

        public DeployController(IMediator mediator)
        {
            _mediator = mediator;
        }
            
        /// <summary>
        /// TODO - this should be moved to a queue / function to offload to the background. Durable function might be a good approach to it to break it into small chunks.
        /// </summary>
        /// <returns></returns>
        [HttpPost("")]
        public Task<ManifestDeployment.DeployResponse> Post(ManifestDeployment.DeployRequest request)
        {
            //Technically we return a operation Id and spin off to the background. But for now this will do.
            return _mediator.Send(request);
        }
    }
}