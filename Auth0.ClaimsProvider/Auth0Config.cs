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

        public List<ClaimAttribute> AttributesList { get; set; }

        public static Auth0Config GetDefaultSettings()
        {
            var config = new Auth0Config();
            config.ClientId = "";
            config.ClientSecret = "";
            config.Domain = "";
            config.AttributesList = new List<ClaimAttribute>
            {
                new ClaimAttribute
                {
                    ClaimType = Microsoft.IdentityModel.Claims.ClaimTypes.Email, 
                    ClaimEntityType = SPClaimEntityTypes.User
                },
            };

            return config;
        }
    }
}