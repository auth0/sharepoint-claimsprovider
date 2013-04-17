namespace Auth0.ClaimsProvider
{
    using System.Collections.Generic;
    using Microsoft.SharePoint.Administration.Claims;

    public class Auth0Config
    {
        public Auth0Config()
        {
        }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string Domain { get; set; }

        public string ConnectionClaimType { get; set; }

        public string Auth0ValueToDisplayForIdentityAttribute { get; set; }

        public bool AlwaysResolveUserInput { get; set; }

        public List<ClaimAttribute> AttributesList { get; set; }

        public static Auth0Config GetDefaultSettings()
        {
            var config = new Auth0Config
            {
                ClientId = "8rag8y1vsf6sTZ29aUJTKSdo4rvECEzk",
                ClientSecret = "EKRE6ShdYvF3ckXJJfSjVNd0PUjI8hbpnkKAhNasUVJHU4Apa7wEL3GfYa6YuGts",
                Domain = "iaco.auth0.com",
                ConnectionClaimType = "http://schemas.auth0.com/connection",
                Auth0ValueToDisplayForIdentityAttribute = null,
                AlwaysResolveUserInput = false,
                AttributesList = new List<ClaimAttribute>
                {
                    new ClaimAttribute
                    {
                        Auth0AttributeName = "Email",
                        ClaimType = Microsoft.IdentityModel.Claims.ClaimTypes.Email, 
                        ClaimEntityType = SPClaimEntityTypes.User
                    }
                }
            };

            return config;
        }
    }
}