using System.Collections.Generic;
using System.Threading.Tasks;
using Speedway.Core.Resources;

namespace Speedway.Deploy.Core.Resources.Speedway.SecretStore
{
    public interface ISecretStorePlatformTwin
    {
        /// <summary>
        /// Some resources create secrets that need storing in the default secret store.
        /// This method must ensure they are present.
        /// </summary>
        /// <param name="secrets"></param>
        /// <param name="externallySetSecrets"></param>
        /// <returns></returns>
        Task StoreKnownSecrets(Dictionary<string, string> secrets, HashSet<string> externallySetSecrets);

        string GetSecretUri(string secretName);

        Task GrantAccessTo(ISpeedwayResource resource, SecretsLink access);
    }
}