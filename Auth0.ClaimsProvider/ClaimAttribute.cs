namespace Auth0.ClaimsProvider
{
    using System;
    using System.Collections.Generic;

    public class ClaimAttribute
    {
        public ClaimAttribute()
        {
            this.ClaimValueType = Microsoft.IdentityModel.Claims.ClaimValueTypes.String;
        }

        public string ClaimType { get; set; }

        public string ClaimValueType { get; set; }

        public string Auth0AttributeName { get; set; }

        /// <summary>
        /// When creating a PickerEntry, it's possible to populate entry with additional attributes stored in EntityData hash table
        /// </summary>
        public string PeopleEditorEntityDataKey { get; set; }

        /// <summary>
        /// What represents the attribute (a user, a role, a security group, etc.)
        /// </summary>
        public string ClaimEntityType { get; set; }

        /// <summary>
        /// Set to true if the attribute should always be queried in Auth0
        /// </summary>
        public bool ResolveAsIdentityClaim { get; set; }

        public string PeoplePickerAttributeHierarchyNodeId { get; set; }

        public string PeoplePickerAttributeDisplayName { get; set; }
    }
}