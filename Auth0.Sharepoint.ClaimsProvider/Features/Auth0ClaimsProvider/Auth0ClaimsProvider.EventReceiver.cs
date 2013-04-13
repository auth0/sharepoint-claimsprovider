namespace Auth0.Sharepoint.ClaimsProvider.Features.Auth0ClaimsProvider
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.SharePoint;
    using Microsoft.SharePoint.Administration.Claims;

    /// <remarks>
    /// The GUID attached to this class may be used during packaging and should not be modified.
    /// </remarks>
    [Guid("e639ef1a-a33f-4dbe-b248-d0857806fbe3")]
    public class Auth0ClaimsProviderFeatureReceiver : SPClaimProviderFeatureReceiver
    {
        public override string ClaimProviderAssembly
        {
            get
            {
                return typeof(Auth0.Sharepoint.ClaimsProvider.Auth0ClaimsProvider).Assembly.FullName;
            }
        }

        public override string ClaimProviderDescription
        {
            get
            {
                return "Auth0 Claims Provider for Sharepoint 2010";
            }
        }

        public override string ClaimProviderDisplayName
        {
            get
            {
                return Auth0.Sharepoint.ClaimsProvider.Auth0ClaimsProvider.DefaultProviderDisplayName;
            }
        }

        public override string ClaimProviderType
        {
            get
            {
                return typeof(Auth0.Sharepoint.ClaimsProvider.Auth0ClaimsProvider).FullName;
            }
        }

        public override void FeatureActivated(SPFeatureReceiverProperties properties)
        {
            this.ExecBaseFeatureActivated(properties);
        }

        private void ExecBaseFeatureActivated(Microsoft.SharePoint.SPFeatureReceiverProperties properties)
        {
            // Wrapper function for base FeatureActivated. Used because base
            // keyword can lead to unverifiable code inside lambda expression.
            base.FeatureActivated(properties);
        }
    }
}