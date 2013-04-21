namespace Auth0.ClaimsProvider.Configuration
{
    using System;
    using System.Collections.Generic;
    using Microsoft.SharePoint.Administration;
    using Microsoft.SharePoint.Administration.Claims;
    using Microsoft.SharePoint.WebControls;

    public class ConfigurationRepository : IConfigurationRepository
    {
        public const string Auth0PersistedObjectName = "Auth0ClaimsProviderConfig";

        public Auth0Config GetConfiguration()
        {
            var configuration = SPFarm.Local.GetChild<Auth0Config>(Auth0PersistedObjectName) ??
                                CreatePersistedObject();

            if (string.IsNullOrEmpty(configuration.PickerEntityGroupName))
            {
                configuration.PickerEntityGroupName = "Results";
            }

            // TODO: move to configuration
            configuration.ConfiguredAttributes = new List<ClaimAttribute>
            {
                new ClaimAttribute
                {
                    Auth0AttributeName = "UniqueEmail",
                    ClaimType = "http://schemas.auth0.com/connection_email", 
                    ClaimEntityType = SPClaimEntityTypes.User
                },
                new ClaimAttribute
                {
                    Auth0AttributeName = "Email",
                    ClaimType = Microsoft.IdentityModel.Claims.ClaimTypes.Email, 
                    ClaimEntityType = SPClaimEntityTypes.User,
                    PeopleEditorEntityDataKey = PeopleEditorEntityDataKeys.Email
                },
                new ClaimAttribute
                {
                    Auth0AttributeName = "Picture",
                    ClaimType = "http://schemas.auth0.com/picture",
                    ClaimEntityType = SPClaimEntityTypes.User,
                    PeopleEditorEntityDataKey = "Picture"
                }
            };

            return configuration;
        }

        public void SaveConfiguration(Auth0Config auth0Config)
        {
            if (auth0Config != null)
            {
                auth0Config.Update();
            }
        }

        private static Auth0Config CreatePersistedObject()
        {
            var persistedObject = new Auth0Config(Auth0PersistedObjectName, SPFarm.Local);
            persistedObject.Update();

            return persistedObject;
        }
    }
}