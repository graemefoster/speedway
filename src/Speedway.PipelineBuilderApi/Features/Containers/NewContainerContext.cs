using System;

namespace Speedway.PipelineBuilderApi.Features.Containers
{
    /// <summary>
    /// A scoped item that flows with the NewContainer command
    /// </summary>
    public class NewContainerContext: ICurrentUserContext
    {
        public string UserName { get; set; }
        public Guid InitialTeamMemberId { get; set; }
        public string InitialTeamMemberEmail { get; set; }
    }
}