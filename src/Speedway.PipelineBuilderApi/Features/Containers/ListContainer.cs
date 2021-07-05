using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Speedway.PipelineBuilderApi.Ports;

namespace Speedway.PipelineBuilderApi.Features.Containers
{
    public static class ListContainer
    {
        public record ListContainersCommand : IRequest<ListContainersResponse>;

        public record ListContainersResponse(Container[] Containers);

        public record Container(Guid Id, string Name, string GitUri);

        // ReSharper disable once UnusedType.Global
        public class ListContainersHandler : IRequestHandler<ListContainersCommand, ListContainersResponse>
        {
            private readonly ISourceControlRepository _repository;

            public ListContainersHandler(
                ISourceControlRepository repository)
            {
                _repository = repository;
            }

            public async Task<ListContainersResponse> Handle(ListContainersCommand request,
                CancellationToken cancellationToken)
            {
                var repositories = await _repository.List();

                return new ListContainersResponse(
                    repositories.Select(x => new Container(
                        x.Id, x.Name, x.Url)).ToArray()
                );
            }
        }
    }
}