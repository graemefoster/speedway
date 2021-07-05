using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Identity.Web;
using Speedway.PipelineBuilderApi.Ports;
using Speedway.PipelineBuilderApi.Ports.Adapters.GraphTypes;

namespace Speedway.PipelineBuilderApi.Features.Containers
{
    public static class NewContainer
    {
        public record NewContainerCommand
            (string Slug, string DisplayName, string InitialDeveloper) : IRequest<NewContainerResponse>;
        public record NewContainerResponse(Guid Id, string GitUri);

        public class NewContainerValidator : AbstractValidator<NewContainerCommand>
        {

            public NewContainerValidator(IDownstreamWebApi downstreamWebApi, NewContainerContext commandContext)
            {
                RuleFor(x => x.InitialDeveloper)
                    .EmailAddress()
                    .CustomAsync(async (s, ctx, token) =>
                    {
                        var aadUser = await downstreamWebApi.GetForUserAsync<DevOpsUsers>("DevOpsEx",  $"_apis/userentitlements?$filter={Uri.EscapeDataString($"name eq '{s}'")}");
                        if ((aadUser?.Items.Length ?? 0) == 0)
                        {
                            ctx.AddFailure("InitialDeveloper", $"Cannot find user {s}");
                        }
                        else
                        {
                            commandContext.InitialTeamMemberEmail = s;
                            commandContext.InitialTeamMemberId = aadUser!.Items.First(x => x.User.PrincipalName == s).Id;
                        }
                    });
            }
        }

        // ReSharper disable once UnusedType.Global
        public class NewContainerHandler : IRequestHandler<NewContainerCommand, NewContainerResponse>
        {
            private readonly ISourceControlRepository _repository;
            private readonly NewContainerContext _context;

            public NewContainerHandler(ISourceControlRepository repository, NewContainerContext context)
            {
                _repository = repository;
                _context = context;
            }

            public async Task<NewContainerResponse> Handle(
                NewContainerCommand request,
                CancellationToken cancellationToken)
            {
                
                var newRepository = await _repository.New(
                    request.Slug, 
                    request.DisplayName,
                    _context.InitialTeamMemberEmail,
                    _context.InitialTeamMemberId);
                
                return new NewContainerResponse(newRepository.Id, newRepository.GitUri);
            }
        }
    }
}