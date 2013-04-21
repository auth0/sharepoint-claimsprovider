namespace Auth0.ClaimsProvider.Configuration
{
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using Microsoft.SharePoint.Administration;

    [GuidAttribute("E3F4059C-DEC6-4887-80E6-2396AA2FE411")]
    public class Auth0Config : SPPersistedObject
    {
        [Persisted]
        private string clientId;

        [Persisted]
        private string clientSecret;

        [Persisted]
        private string domain;

        [Persisted]
        private bool alwaysResolveUserInput;

        [Persisted]
        private string pickerEntityGroupName;

        public Auth0Config()
        {
        }

        public Auth0Config(string objectName, SPPersistedObject parent)
            : base(objectName, parent)
        {
            this.ClientId = string.Empty;
            this.ClientSecret = string.Empty;
            this.Domain = string.Empty;
            this.PickerEntityGroupName = string.Empty;
            this.ConfiguredAttributes = new List<ClaimAttribute>();
        }

        public string ClientId
        {
            get { return this.clientId; }

            set { this.clientId = value; }
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

        public string PickerEntityGroupName
        {
            get { return this.pickerEntityGroupName; }

            set { this.pickerEntityGroupName = value; }
        }

        public List<ClaimAttribute> ConfiguredAttributes { get; set; }
    }
}