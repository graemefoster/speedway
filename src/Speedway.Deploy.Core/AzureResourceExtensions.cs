using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Graph.RBAC.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Speedway.AzureSdk.Extensions;

namespace Speedway.Deploy.Core
{
    public static class AzureResourceExtensions
    {
        public const string StorageBlobDataContributorRoleId = "ba92f5b4-2d11-453d-a403-e96b0029c9fe";
        public const string StorageBlobDataReaderRoleId = "2a2b9908-6ea1-4ae2-8e65-a410df84e7d1";
        public const string StorageQueueDataContributorRoleId = "974c5e8b-45b9-4653-ba55-5f855dd0fb88";
        public const string StorageQueueDataReaderRoleId = "19e7f393-937e-4f77-808e-94535e297925";
        public const string KeyVaultSecretsUser = "4633458b-17de-408a-b874-0445c86b69e6";
        public const string KeyVaultSecretsReader = "21090545-7ca7-4776-b22c-e363652d74d2";
        public const string KeyVaultSecretsOfficer = "b86a8fe4-44ce-4948-aee5-eccb2c155cd7";
        public const string UserAccessAdministratorRoleId = "18d7d88d-d35e-4fb5-a5c3-7773c20a72d9";

        public static async Task ReflectRolesOnSubscription(this IAzure az,
            ServicePrincipal servicePrincipal,
            ILogger logger,
            params string[] roles)
        {
            var scope = $"subscriptions/{az.SubscriptionId}";
            var rolesToAssign = await Task.WhenAll(roles.Select(async x => await az.AccessManagement.RoleDefinitions.GetByScopeAndRoleNameAsync($"subscriptions/{az.SubscriptionId}", x)));

            var existingRoleAssigment = await az.AccessManagement.RoleAssignments.ListByScopeAsync(scope);
            foreach (var role in rolesToAssign)
            {
                if (existingRoleAssigment.Any(x => x.RoleDefinitionId == role.Id && x.PrincipalId == servicePrincipal.Id))
                {
                    logger.LogDebug("Principal {principal} already has access to subscription {resource}",
                        servicePrincipal.Id, scope);
                }
                else
                {
                    logger.LogInformation("Granting Principal {principal} role {role} on subscription {resource}",
                        servicePrincipal.DisplayName, role.Name, scope);

                    await az.AccessManagement.RoleAssignments.Define(Guid.NewGuid().ToString())
                        .ForServicePrincipal(servicePrincipal.AppId)
                        .WithRoleDefinition(role.Id)
                        .WithSubscriptionScope(az.SubscriptionId)
                        .CreateAsync();
                }
            }
        }

        public static async Task ReflectRolesOnResource(
            this IAzure az,
            IResource resource,
            ServicePrincipal servicePrincipal,
            ILogger logger,
            params string[] roles)
        {
            var rolesToAssign = await Task.WhenAll(roles.Select(async x => await az.AccessManagement.RoleDefinitions.GetByScopeAsync($"subscriptions/{az.SubscriptionId}",x)));
            var existingRoleAssigment = await az.AccessManagement.RoleAssignments.ListByScopeAsync(resource.Id);
            
            foreach (var role in rolesToAssign)
            {
                if (existingRoleAssigment.Any(x =>
                    x.RoleDefinitionId == role.Id && x.PrincipalId == servicePrincipal.Id))
                {
                    logger.LogDebug("Principal {Principal} already has access to resource {Resource}",
                        servicePrincipal.Id, resource.Name);
                }
                else
                {
                    logger.LogInformation("Granting Principal {Principal} role {Role} on resource {Resource}",
                        servicePrincipal.DisplayName, role.Name, resource.Name);

                    //ensure sp exists:
                    var exists = false;
                    while (!exists)
                    {
                        await az.WaitForAz(x => az.AccessManagement.ServicePrincipals.GetByIdAsync(servicePrincipal.Id));
                        exists = true;
                    }

                    await az.AccessManagement.RoleAssignments.Define(Guid.NewGuid().ToString())
                        .ForServicePrincipal(servicePrincipal.AppId)
                        .WithRoleDefinition(role.Id)
                        .WithResourceScope(resource)
                        .CreateAsync();
                }
            }
        }
    }
}