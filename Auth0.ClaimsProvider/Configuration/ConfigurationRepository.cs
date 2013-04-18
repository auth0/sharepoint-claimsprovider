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

            try
            {
                var configuration = parent.GetChild<Auth0Config>(Auth0Config.Auth0PersistedObjectName) ?? 
                                    CreatePersistedObject();

                // TODO: remove this
                configuration.Auth0ValueToDisplayForIdentityAttribute = null;
                configuration.AttributesList = new List<ClaimAttribute>
                {
                    new ClaimAttribute
                    {
                        Auth0AttributeName = "Email",
                        ClaimType = Microsoft.IdentityModel.Claims.ClaimTypes.Email, 
                        ClaimEntityType = SPClaimEntityTypes.User
                    }
                };
            }
            catch (Exception)
            {
                // string.Format("Error while retrieving SPPersistedObject {0}: {1}", Auth0PersistedObjectName, ex.Message)
            }

            return null;
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

        private static Auth0Config GetDefaultSettings()
        {
            var config = new Auth0Config
            {
                ClientId = "8rag8y1vsf6sTZ29aUJTKSdo4rvECEzk",
                ClientSecret = "EKRE6ShdYvF3ckXJJfSjVNd0PUjI8hbpnkKAhNasUVJHU4Apa7wEL3GfYa6YuGts",
                Domain = "iaco.auth0.com",
                AlwaysResolveUserInput = false
            };

            return config;
        }
    }
}