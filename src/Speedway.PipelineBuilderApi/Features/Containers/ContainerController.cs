using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;

namespace Speedway.PipelineBuilderApi.Features.Containers
{
    // [Authorize]
    [Route("/[controller]")]
    [Authorize]
    [ApiController]
    public class ContainerController: ControllerBase
    {
        private readonly IMediator _mediator;

        public ContainerController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("")]
        public async Task<NewContainer.NewContainerResponse> Post(NewContainer.NewContainerCommand command)
        {
            HttpContext.VerifyUserHasAnyAcceptedScope("user_impersonation");
            return await _mediator.Send(command);
        }

        [HttpGet("")]
        public async Task<ListContainer.ListContainersResponse> Get()
        {
            return await _mediator.Send(new ListContainer.ListContainersCommand());
        }
    }
}