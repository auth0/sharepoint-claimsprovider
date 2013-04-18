namespace Auth0.ClaimsProvider.Configuration
{
    using System.Collections.Generic;
    using Microsoft.SharePoint.Administration;

    public class Auth0Config : SPPersistedObject
    {
        public const string Auth0PersistedObjectName = "Auth0ClaimsProviderConfig";
        public const string Auth0PersistedObjectNameId = "E3F4059C-DEC6-4887-80E6-2396AA2FE411";

        [Persisted]
        private string cliendId;

        [Persisted]
        private string clientSecret;

        [Persisted]
        private string domain;

        [Persisted]
        private bool alwaysResolveUserInput;

        public Auth0Config()
        {
        }

        public Auth0Config(SPPersistedObject parent)
            : base(Auth0PersistedObjectName, parent)
        {
        }

        public string ClientId
        {
            get { return this.cliendId; }

            set { this.cliendId = value; }
        }

        public string ClientSecret
        {
            get { return this.clientSecret; }

            set { this.clientSecret = value; }
        }

        public string Domain
        {
            get { return this.domain; }

            set { this.domain = value; }
        }

        public bool AlwaysResolveUserInput
        {
            get { return this.alwaysResolveUserInput; }

            set { this.alwaysResolveUserInput = value; }
        }

        public string Auth0ValueToDisplayForIdentityAttribute { get; set; }

        public List<ClaimAttribute> AttributesList { get; set; }
    }
}