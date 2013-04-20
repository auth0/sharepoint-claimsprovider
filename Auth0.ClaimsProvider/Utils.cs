namespace Auth0.ClaimsProvider
{
    using System.Linq;
    using Microsoft.SharePoint.Administration;
    using Microsoft.SharePoint.Administration.Claims;

    public class Utils
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
                    LogToULS(
                        string.Format("Claim provider '{0}' is associated to several TrustedLoginProvider, which is not supported because there is no way to determine what TrustedLoginProvider is currently calling the claim provider during search and resolution.", providerInternalName),
                        TraceSeverity.Unexpected, 
                        EventSeverity.Error);
                }
            }

            LogToULS(
                string.Format("Claim provider '{0}' is not associated with any SPTrustedLoginProvider, and it cannot create permissions for a trust if it is not associated to it.\r\nUse PowerShell cmdlet Get-SPTrustedIdentityTokenIssuer to create association", providerInternalName),
                TraceSeverity.Unexpected, 
                EventSeverity.Warning);
            
            return null;
        }

        public static void LogToULS(string message, TraceSeverity traceSeverity, EventSeverity eventSeverity)
        {
            try
            {
                var category = new SPDiagnosticsCategory(CustomClaimsProvider.ProviderInternalName, traceSeverity, eventSeverity);
                var ds = SPDiagnosticsService.Local;
                ds.WriteTrace(0, category, traceSeverity, message);
            }
            catch
            {
            }
        }

        public static object GetPropertyValue(object src, string propertyName)
        {
            var property = src.GetType().GetProperty(propertyName);
            if (property == null)
            {
                // Look for a method
                var method = src.GetType().GetMethod(propertyName);
                return method != null ? method.Invoke(src, null) : string.Empty;
            }

            return property.GetValue(src, null);
        }
    }
}