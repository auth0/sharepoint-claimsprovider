namespace Auth0.ClaimsProvider
{
    using System;
    using System.Linq;
    using Microsoft.SharePoint.Administration.Claims;

    public class Helper
    {
        /// <summary>
        /// Get the first TrustedLoginProvider associated with current claim provider
        /// </summary>
        /// <param name="ProviderInternalName"></param>
        /// <returns></returns>
        public static SPTrustedLoginProvider GetSPTrustAssociatedWithCP(string providerInternalName)
        {
            var providers = SPSecurityTokenServiceManager.Local.TrustedLoginProviders.Where(p => p.ClaimProviderName == providerInternalName);

            if (providers != null && providers.Count() > 0)
            {
                if (providers.Count() == 1)
                {
                    return providers.First();
                }
                else
                {
                    // string.Format("Claim provider '{0}' is associated to several TrustedLoginProvider, which is not supported because there is no way to determine what TrustedLoginProvider is currently calling the claim provider during search and resolution.", providerInternalName));
                }
            }

            // string.Format("Claim provider '{0}' is not associated with any SPTrustedLoginProvider, and it cannot create permissions for a trust if it is not associated to it.\r\nUse PowerShell cmdlet Get-SPTrustedIdentityTokenIssuer to create association", ProviderInternalName)
            return null;
        }
    }
}