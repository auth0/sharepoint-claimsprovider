namespace Auth0.ClaimsProvider.Configuration
{
    using System;
    using System.Collections.Generic;
    using Microsoft.SharePoint.Administration;
    using Microsoft.SharePoint.Administration.Claims;

    public class ConfigurationRepository : IConfigurationRepository
    {
        public Auth0Config GetConfiguration()
        {
            SPPersistedObject parent = SPFarm.Local;

            var configuration = parent.GetChild<Auth0Config>(Auth0Config.Auth0PersistedObjectName) ??
                                CreatePersistedObject();

            if (string.IsNullOrEmpty(configuration.PickerEntityGroupName.Trim()))
            {
                configuration.PickerEntityGroupName = "Results";
            }

            // TODO: move to configuration
            configuration.AttributesToShow = new List<ClaimAttribute>
                {
                    new ClaimAttribute
                    {
                        Auth0AttributeName = "Email",
                        ClaimType = Microsoft.IdentityModel.Claims.ClaimTypes.Email, 
                        ClaimEntityType = SPClaimEntityTypes.User
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
            var persistedObject = new Auth0Config(SPFarm.Local);
            persistedObject.Id = new Guid(Auth0Config.Auth0PersistedObjectNameId);
            persistedObject.Update();

            // string.Format("Created PersistedObject {0} with Id {1}", persistedObject.Name, persistedObject.Id);
            return persistedObject;
        }
    }
}