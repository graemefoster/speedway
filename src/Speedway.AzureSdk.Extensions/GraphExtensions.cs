using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Graph.RBAC.Fluent.Models;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using RequiredResourceAccess = Microsoft.Graph.RequiredResourceAccess;
using ResourceAccess = Microsoft.Graph.ResourceAccess;

namespace Speedway.AzureSdk.Extensions
{
    public static class GraphExtensions
    {
        /// <summary>
        /// Request application roles for a service principal
        /// </summary>
        /// <param name="graphClient"></param>
        /// <param name="servicePrincipalExposingRole"></param>
        /// <param name="servicePrincipalRequestingAccess"></param>
        /// <param name="roles">Either the Ids, or the values of the roles as exposed by servicePrincipalExposingRole</param>
        /// <returns></returns>
        public static async Task ApplyRoles(this IGraphServiceClient graphClient, ServicePrincipal servicePrincipalExposingRole, ServicePrincipal servicePrincipalRequestingAccess, params string[] roles)
        {
            var app = graphClient.Applications.Request().Filter($"appId eq '{servicePrincipalRequestingAccess.AppId}'").GetAsync().Result.SingleOrDefault();
            if (app != null)
            {
                await graphClient.ApplyConfiguredPermissionsToApplication(
                    servicePrincipalExposingRole,
                    app,
                    roles);
            }
            else
            {
                await AssignRolesToActiveDirectoryId(graphClient, servicePrincipalExposingRole, roles, Guid.Parse(servicePrincipalRequestingAccess.Id), "ServicePrincipal");
            }
        }

        public static async Task ApplyScopes(this IGraphServiceClient graphClient, ServicePrincipal servicePrincipalExposingScope, ServicePrincipal servicePrincipalRequestingAccess, params string[] scopes)
        {
            Guid GetScopeId(string scope)
            {
                if (Guid.TryParse(scope, out var g)) return g;
                var permissionScope = servicePrincipalExposingScope.Oauth2PermissionScopes?.SingleOrDefault(appScope => appScope.Value == scope)?.Id;
                if (permissionScope == null) throw new ArgumentException($"Failed to find scope {scope} on service principal {servicePrincipalExposingScope.DisplayName} ({servicePrincipalExposingScope.AppId}).");
                return permissionScope.Value;
            }
            
            var applicationRequestingRole = await graphClient.GetApplicationSafe(servicePrincipalRequestingAccess.AppId);
            var thisRoleWouldBe = new RequiredResourceAccess()
            {
                ResourceAppId = servicePrincipalExposingScope.AppId,
                ResourceAccess = scopes.Select(x => new ResourceAccess()
                {
                    Id = GetScopeId(x),
                    Type = "Scope"
                })
            };

            var newScopes = applicationRequestingRole.RequiredResourceAccess.ToArray();
            if (applicationRequestingRole.RequiredResourceAccess.All(x => x.ResourceAppId != thisRoleWouldBe.ResourceAppId))
            {
                newScopes = newScopes.Concat(new[] {thisRoleWouldBe}).ToArray();
            }
            else
            {
                var existingRole = newScopes.Single(x => x.ResourceAppId == thisRoleWouldBe.ResourceAppId);
                var newPermissions = thisRoleWouldBe.ResourceAccess.Where(newPerm =>
                    existingRole.ResourceAccess.All(existing => existing.Id != newPerm.Id));
                existingRole.ResourceAccess = existingRole.ResourceAccess.Concat(newPermissions).ToArray();
            }

            var updated = new Application()
            {
                RequiredResourceAccess = newScopes
            };

            await graphClient.Applications[applicationRequestingRole.Id].Request().UpdateAsync(updated);
        }

        public static async Task<Application> GetApplicationSafe(this IGraphServiceClient graphClient, string azureIdentityApplicationId)
        {
            var application = await graphClient.WaitForGraph(async x => await x.Applications.Request().Filter($"appId eq '{azureIdentityApplicationId}'").GetAsync());
            return application.Single();
        } 
        public static async Task<ServicePrincipal> GetApplicationServicePrincipalSafe(this IGraphServiceClient graphClient, string azureIdentityApplicationId)
        {
            var servicePrincipal = await graphClient.WaitForGraph(async x => await x.ServicePrincipals.Request().Filter($"appId eq '{azureIdentityApplicationId}'").GetAsync());
            return servicePrincipal.Single();
        }
        
        public static async Task<ServicePrincipal> GetServicePrincipalSafe(this IGraphServiceClient graphClient, string azureIdentityServicePrincipalid)
        {
            var servicePrincipal = await graphClient.WaitForGraph(async x => await x.ServicePrincipals[azureIdentityServicePrincipalid].Request().GetAsync());
            return servicePrincipal;
        }

        public static async Task ApplyRolesToUser(this IGraphServiceClient graphClient, ILogger logger, ServicePrincipal servicePrincipalExposingRole, string userEmail, string[] roles)
        {
            var user = await graphClient.WaitForGraph(async x => (await x.Users.Request().Filter($"userprincipalname eq '{Uri.EscapeDataString(userEmail)}'").GetAsync()).Single());
            await AssignRolesToActiveDirectoryId(graphClient, servicePrincipalExposingRole, roles, Guid.Parse(user.Id), "User");
        }

        public static async Task ApplyRolesToGroup(this IGraphServiceClient graphClient, ILogger logger, ServicePrincipal servicePrincipalExposingRole, string groupName, string[] roles)
        {
            var group = await graphClient.WaitForGraph(async x => (await x.Groups.Request().Filter($"displayName eq '{Uri.EscapeDataString(groupName)}'").GetAsync()).Single());
            await AssignRolesToActiveDirectoryId(graphClient, servicePrincipalExposingRole, roles, Guid.Parse(group.Id), "Group");
        }

        private static async Task AssignRolesToActiveDirectoryId(IGraphServiceClient graphClient, ServicePrincipal servicePrincipalExposingRole, string[] roles, Guid id, string idType)
        {
            var roleIds = GetRoleIds(servicePrincipalExposingRole, roles);

            var existingAssignments = await graphClient.ServicePrincipals[servicePrincipalExposingRole.Id].AppRoleAssignments.Request().GetAsync();
            var assignmentsToThisRoleForThisGroup = existingAssignments.Where(x => x.PrincipalId == id);
            var missingRoles = roleIds.Where(x => assignmentsToThisRoleForThisGroup.All(r => r.AppRoleId != x));
            foreach (var newRole in missingRoles)
            {
                var newRoleAssignment = new AppRoleAssignment()
                {
                    PrincipalId = id,
                    ResourceId = Guid.Parse(servicePrincipalExposingRole.Id),
                    AppRoleId = newRole,
                    PrincipalType = idType
                };
                try
                {
                    await graphClient.ServicePrincipals[servicePrincipalExposingRole.Id].AppRoleAssignments.Request()
                        .AddAsync(newRoleAssignment);
                }
                catch (ServiceException se) when (se.StatusCode == HttpStatusCode.BadRequest &&
                                                  se.Error.Details.First().Code == "InvalidUpdate")
                {
                } 
            }
        }

        private static Guid[] GetRoleIds(ServicePrincipal servicePrincipalExposingRole, string[] roles)
        {
            var roleIds = roles.Select(
                    requestedRole => servicePrincipalExposingRole.AppRoles.Single(
                        appRole => appRole.Id != null && (appRole.Id.Value.ToString() == requestedRole ||
                                   appRole.Value == requestedRole)))
                .Select(appRole => appRole.Id!.Value).ToArray();
            return roleIds;
        }

        /// <summary>
        /// Request application roles for a service principal
        /// </summary>
        /// <returns></returns>
        private static async Task ApplyConfiguredPermissionsToApplication(this IGraphServiceClient graphClient, ServicePrincipal servicePrincipalExposingRole, Application applicationWantingAccess, params string[] roles)
        {
            var roleIds = GetRoleIds(servicePrincipalExposingRole, roles);

            var thisRoleWouldBe = new RequiredResourceAccess
            {
                ResourceAppId = servicePrincipalExposingRole.AppId,
                ResourceAccess = roleIds.Select(x => new ResourceAccess
                {
                    Id = x,
                    Type = "Role"
                })
            };

            var newRoles = applicationWantingAccess.RequiredResourceAccess.ToArray();
            if (applicationWantingAccess.RequiredResourceAccess.All(x => x.ResourceAppId != thisRoleWouldBe.ResourceAppId))
            {
                newRoles = newRoles.Concat(new[] {thisRoleWouldBe}).ToArray();
            }
            else
            {
                var existingRole = newRoles.Single(x => x.ResourceAppId == thisRoleWouldBe.ResourceAppId);
                var newPermissions = thisRoleWouldBe.ResourceAccess.Where(newPerm =>
                    existingRole.ResourceAccess.All(existing => existing.Id != newPerm.Id));
                existingRole.ResourceAccess = existingRole.ResourceAccess.Concat(newPermissions).ToArray();
            }

            var updated = new Application()
            {
                RequiredResourceAccess = newRoles
            };

            await graphClient.Applications[applicationWantingAccess.Id].Request().UpdateAsync(updated);
        }
        
        /// <summary>
        /// Eventual consistency on the graph
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <param name="getter"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static async Task<T> WaitForGraph<T>(this IGraphServiceClient graphServiceClient,
            Func<IGraphServiceClient, Task<T>> getter)
        {
            var attempts = 5;
            while (attempts > 0)
            {
                try
                {
                    var item = await getter(graphServiceClient);
                    if (item != null)
                    {
                        return item;
                    }
                }
                catch (ServiceException se) when (se.StatusCode == HttpStatusCode.NotFound)
                {
                }

                attempts = attempts - 1;
                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            throw new InvalidOperationException("Graph didn't catch up in time.");
        }

        /// <summary>
        /// Eventual consistency on the az / graph
        /// </summary>
        /// <param name="az"></param>
        /// <param name="getter"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static async Task<T> WaitForAz<T>(this IAzure az, Func<IAzure, Task<T>> getter)
        {
            var attempts = 5;
            while (attempts > 0)
            {
                try
                {
                    var item = await getter(az);
                    if (item != null)
                    {
                        return item;
                    }
                }
                catch (GraphErrorException se) when (se.Response.StatusCode == HttpStatusCode.NotFound)
                {
                }

                attempts = attempts - 1;
                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            throw new InvalidOperationException("Graph didn't catch up in time.");
        }

        /// <summary>
        /// Eventual consistency on the az / graph
        /// </summary>
        /// <param name="vault"></param>
        /// <param name="getter"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static async Task<T> WaitForKvPermissions<T>(this IVault vault, Func<IVault, Task<T>> getter)
        {
            var attempts = 5;
            while (attempts > 0)
            {
                try
                {
                    var item = await getter(vault);
                    if (item != null)
                    {
                        return item;
                    }
                }
                catch (KeyVaultErrorException se) when (se.Response.StatusCode == HttpStatusCode.Forbidden)
                {
                }

                attempts = attempts - 1;
                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            throw new InvalidOperationException("Graph didn't catch up in time.");
        }

    }
}