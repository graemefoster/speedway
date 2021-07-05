using System;
using System.Threading.Tasks;
using Project = Speedway.PipelineBuilderApi.Domain.Project;

namespace Speedway.PipelineBuilderApi.Ports
{
    public interface ISourceControlRepository
    {
        Task<NewRepositoryResponse> New(string slug, string displayName, string initialTeamMemberEmail, Guid initialUserId);
        Task<Project[]> List();
    }
}