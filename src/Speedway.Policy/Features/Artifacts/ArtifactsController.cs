using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

namespace Speedway.Policy.Features.Artifacts
{
    [Authorize(Roles = "Risk,Security,Tester")]
    [AuthorizeForScopes(Scopes = new [] {"Artifacts.ReadWriteAll"})]
    [ApiController]
    [Route("/[controller]")]
    public class ArtifactsController: ControllerBase
    {
        private readonly IMediator _mediator;

        public ArtifactsController(IMediator mediator)
        {
            _mediator = mediator;
        }
        
        [HttpPost]
        public Task<NewArtifact.Response> Post(NewArtifact.Request command)
        {
            return _mediator.Send(command);
        }
    }

}