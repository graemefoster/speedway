using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Speedway.Core.Resources;
using Speedway.Deploy.Core.Resources.Speedway.Context;
using Speedway.Deploy.Core.Resources.Speedway.WebApp;

namespace Speedway.Deploy.Core.Resources.Speedway.OAuthClient
{
    /// <summary>
    /// Represents a component which runs as an App / API.
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public class SpeedwayOAuthClientResource : SpeedwayResource<SpeedwayOAuthClientResourceMetadata>, ISpeedwayResourceThatSupportsLinkingTo
    {
        public SpeedwayOAuthClientResource(
            SpeedwayContextResource context, 
            SpeedwayOAuthClientResourceMetadata metadata, 
            ISpeedwayPlatformTwinFactory twinFactory) : base(context, metadata, twinFactory)
        {
        }

        /// <summary>
        /// Check for any runtime access we need to configure for this OAuth resource
        /// </summary>
        /// <param name="metadata"></param>
        /// <returns></returns>
        public override async Task PostProcess(IDictionary<ISpeedwayResource, SpeedwayResourceOutputMetadata> metadata)
        {
            await SetKnownApplicationsWhoImplicitlyConsentToThisApi(metadata);
            await AddRedirectUris(metadata);
            await base.PostProcess(metadata);
        }

        /// <summary>
        /// Allows another application to implicitly grant consent for this application. Commonly used in OAuth on-behalf-of-flows
        /// </summary>
        /// <param name="speedwayResourceOutputMetadata"></param>
        /// <returns></returns>
        private async Task SetKnownApplicationsWhoImplicitlyConsentToThisApi(
            IDictionary<ISpeedwayResource, SpeedwayResourceOutputMetadata> speedwayResourceOutputMetadata)
        {
            if (ResourceMetadata.KnownApplications?.Any() ?? false)
            {
                var myTwin = GetPlatformTwin<ISpeedwayOAuthClientPlatformTwin>();
                foreach (var knownApplication in ResourceMetadata.KnownApplications)
                {
                    var application = speedwayResourceOutputMetadata.Keys.FindSpeedwayOAuthClient(knownApplication)!.PlatformTwin;
                    await myTwin.SetKnownApplications(application);
                }
            }
        }

        private async Task AddRedirectUris(IDictionary<ISpeedwayResource, SpeedwayResourceOutputMetadata> metadata)
        {
            var redirectUris = ResourceMetadata.RedirectsFrom?.Select(x =>
                ((SpeedwayWebAppResourceOutputMetadata) metadata[
                    metadata.Keys.Single(r => r.Name == x)]).Uri + "signin-oidc").ToArray() ?? Array.Empty<string>();

            if (redirectUris.Any())
            {
                await GetPlatformTwin<ISpeedwayOAuthClientPlatformTwin>().AddRedirects(redirectUris);
            }
        }


        /// <summary>
        /// Called when 'resource' wants access to the link.
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="linkInformation"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public Task GrantAccessTo(ISpeedwayResource resource, SpeedwayResourceLinkMetadata linkInformation)
        {
            var roleAccess = linkInformation as OAuthRoleLink;
            if (roleAccess != null)
            {
                return GetPlatformTwin<ISpeedwayOAuthClientPlatformTwin>().RequestApplicationAccess(roleAccess.Roles, resource);
            }
            var scopeAccess = linkInformation as OAuthScopeLink;
            if (scopeAccess != null)
            {
                return GetPlatformTwin<ISpeedwayOAuthClientPlatformTwin>().RequestDelegatedAccess(scopeAccess.Scopes, resource);
            }

            throw new NotSupportedException(
                $"OAuth Client does not support links of type {linkInformation.GetType().Name}");

        }
    }
}