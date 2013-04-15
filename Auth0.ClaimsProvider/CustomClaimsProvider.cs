namespace Auth0.ClaimsProvider
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Microsoft.SharePoint;
    using Microsoft.SharePoint.Administration.Claims;
    using Microsoft.SharePoint.WebControls;

    public class CustomClaimsProvider : SPClaimProvider
    {
        private SPTrustedLoginProvider associatedSPTrustedLoginProvider; // Name of the SPTrustedLoginProvider associated with the claim provider
        private Auth0Config auth0Config;
        private ClaimAttribute identityAttribute; // attribute mapped to the identity claim in the SPTrustedLoginProvider
        private IEnumerable<ClaimAttribute> attributesToQuery;
        private IEnumerable<ClaimAttribute> attributesDefinitionList;
        private IEnumerable<ConsolidatedResult> consolidatedResults;

        public CustomClaimsProvider(string displayName)
            : base(displayName)
        {
            // SPContext.Current is null in the STS, and there is nothing to do in the STS
            if (SPContext.Current == null)
            {
                return;
            }

            this.Initialize();
        }

        public override string Name
        {
            get
            {
                return ProviderInternalName;
            }
        }

        public override bool SupportsEntityInformation
        {
            get { return false; }
        }

        public override bool SupportsHierarchy
        {
            get { return true; }
        }

        public override bool SupportsResolve
        {
            get { return true; }
        }

        public override bool SupportsSearch
        {
            get { return true; }
        }

        internal static string ProviderDisplayName
        {
            get { return "Federated Users (Auth0)"; }
        }

        internal static string ProviderInternalName
        {
            get { return "Federated Users (Auth0)"; }
        }

        /// <summary>
        /// List of attributes actually defined in the trust + list of Auth0 attributes that are always queried
        /// </summary>
        protected IEnumerable<ClaimAttribute> AttributesToQuery
        {
            get { return this.attributesToQuery; }
        }

        /// <summary>
        /// List of attributes with the claim type they are associated with.
        /// The claim provider will only search against attributes in this list.
        /// </summary>
        protected IEnumerable<ClaimAttribute> AttributesDefinitionList
        {
            get { return this.attributesDefinitionList; }
        }

        protected override void FillClaimTypes(List<string> claimTypes)
        {
            if (claimTypes == null)
            {
                throw new ArgumentNullException("claimTypes");
            }

            if (this.attributesToQuery == null)
            {
                return;
            }

            foreach (var attribute in this.attributesToQuery.Where(a => !string.IsNullOrEmpty(a.ClaimType)))
            {
                claimTypes.Add(attribute.ClaimType);
            }
        }

        protected override void FillClaimValueTypes(List<string> claimValueTypes)
        {
            if (claimValueTypes == null)
            {
                throw new ArgumentNullException("claimValueTypes");
            }

            if (this.attributesToQuery == null)
            {
                return;
            }

            foreach (var attribute in this.attributesToQuery.Where(a => !string.IsNullOrEmpty(a.ClaimValueType)))
            {
                claimValueTypes.Add(attribute.ClaimValueType);
            }
        }

        protected override void FillClaimsForEntity(Uri context, SPClaim entity, List<SPClaim> claims)
        {
            throw new NotImplementedException();
        }

        protected override void FillEntityTypes(List<string> entityTypes)
        {
            if (this.attributesToQuery == null)
            {
                return;
            }

            var uniqueEntitytypes = from attributes in this.attributesToQuery
                                    where attributes.ClaimEntityType != null
                                    group attributes by new { attributes.ClaimEntityType } into groupedByEntityType
                                    select new { value = groupedByEntityType.Key.ClaimEntityType };

            if (uniqueEntitytypes == null)
            {
                return;
            }

            foreach (var entityType in uniqueEntitytypes)
            {
                entityTypes.Add(entityType.value);
            }
        }

        protected override void FillHierarchy(Uri context, string[] entityTypes, string hierarchyNodeID, int numberOfLevels, SPProviderHierarchyTree hierarchy)
        {
            // Ensure that People Picker is asking for the type of entity that we return; site collection administrator will not return, for example.
            if (!CustomClaimsProvider.EntityTypesContain(entityTypes, SPClaimEntityTypes.FormsRole))
            {
                return;
            }

            if (this.attributesToQuery == null)
            {
                return;
            }

            if (hierarchyNodeID == null)
            {
                // First load
                foreach (var attribute in this.attributesToQuery.Where(
                    a => !string.IsNullOrEmpty(a.PeoplePickerAttributeHierarchyNodeId) && 
                         !a.ResolveAsIdentityClaim && entityTypes.Contains(a.ClaimEntityType)))
                {
                    hierarchy.AddChild(
                        new Microsoft.SharePoint.WebControls.SPProviderHierarchyNode(
                            ProviderInternalName,
                            attribute.PeoplePickerAttributeDisplayName,
                            attribute.PeoplePickerAttributeHierarchyNodeId,
                            true));
                }
            }
        }

        protected override void FillResolve(Uri context, string[] entityTypes, SPClaim resolveInput, List<PickerEntity> resolved)
        {
            // Ensure that People Picker is asking for the type of entity that we return; site collection administrator will not return, for example.
            if (!CustomClaimsProvider.EntityTypesContain(entityTypes, SPClaimEntityTypes.FormsRole))
            {
                return;
            }

            if (!string.Equals(
                resolveInput.OriginalIssuer,
                SPOriginalIssuers.Format(SPOriginalIssuerType.TrustedProvider, this.associatedSPTrustedLoginProvider.Name),
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (this.attributesToQuery == null)
            {
                return;
            }

            SPSecurity.RunWithElevatedPrivileges(delegate
            {
                this.Initialize();

                // Resolve value only against the incoming claim type that uniquely identifies the user (mail, userName, etc)
                var attributes = this.attributesToQuery.Where(a => a.ClaimType == resolveInput.ClaimType && !a.ResolveAsIdentityClaim);

                if (attributes.Count() != 1)
                {
                    // Should always find only 1 attribute at this stage
                    // string.Format("Found {0} attributes that match the claim type \"{1}\", but only 1 is expected. Verify that there is no duplicate claim type. Skipping resolution of the claim.", ProviderDisplayName, attributes.Count().ToString(), resolveInput.ClaimType)
                    return;
                }

                this.ResolveInputBulk(resolveInput.Value, attributes, true);
                if (this.consolidatedResults != null && this.consolidatedResults.Count() > 0)
                {
                    resolved.Add(this.consolidatedResults.ElementAt(0).PickerEntity);
                    return;
                }
            });
        }

        protected override void FillResolve(Uri context, string[] entityTypes, string resolveInput, List<PickerEntity> resolved)
        {
            // Ensure that People Picker is asking for the type of entity that we return; site collection administrator will not return, for example.
            if (!CustomClaimsProvider.EntityTypesContain(entityTypes, SPClaimEntityTypes.FormsRole))
            {
                return;
            }

            if (this.attributesToQuery == null)
            {
                return;
            }

            string input = resolveInput;
            SPSecurity.RunWithElevatedPrivileges(delegate
            {
                this.Initialize();

                IEnumerable<ClaimAttribute> attributeCollection = this.attributesToQuery.Where(
                    a => entityTypes.Contains(a.ClaimEntityType) && !a.ResolveAsIdentityClaim);

                this.ResolveInputBulk(input, attributeCollection, false);

                if (this.consolidatedResults != null && this.consolidatedResults.Count() > 0)
                {
                    foreach (var result in this.consolidatedResults)
                    {
                        resolved.Add(result.PickerEntity);
                    }
                }
            });
        }

        protected override void FillSchema(SPProviderSchema schema)
        {
        }

        protected override void FillSearch(Uri context, string[] entityTypes, string searchPattern, string hierarchyNodeID, int maxCount, SPProviderHierarchyTree searchTree)
        {
            // Ensure that People Picker is asking for the type of entity that we return; site collection administrator will not return, for example.
            if (!CustomClaimsProvider.EntityTypesContain(entityTypes, SPClaimEntityTypes.FormsRole))
            {
                return;
            }

            if (this.attributesToQuery == null)
            {
                return;
            }

            SPProviderHierarchyNode matchNode = null;
            SPSecurity.RunWithElevatedPrivileges(delegate
            {
                this.Initialize();

                IEnumerable<ClaimAttribute> attributeCollection;
                if (!string.IsNullOrEmpty(hierarchyNodeID))
                {
                    // Restrict search to attribute currently selected in the hierarchy
                    attributeCollection = this.attributesToQuery.Where(
                        a => a.PeoplePickerAttributeHierarchyNodeId == hierarchyNodeID && entityTypes.Contains(a.ClaimEntityType));

                    // If currently selected attribute is identity attribute then add Auth0 attributes that should always be queried
                    if (attributeCollection.Contains(this.identityAttribute))
                    {
                        attributeCollection = attributeCollection.Union(this.attributesToQuery.Where(a => a.ResolveAsIdentityClaim));
                    }
                }
                else
                {
                    attributeCollection = this.attributesToQuery.Where(a => entityTypes.Contains(a.ClaimEntityType) || a.ResolveAsIdentityClaim);
                }

                this.ResolveInputBulk(searchPattern, attributeCollection, false);
                if (this.consolidatedResults != null && this.consolidatedResults.Count() > 0)
                {
                    foreach (var consolidatedResult in this.consolidatedResults)
                    {
                        // Add current PickerEntity to the corresponding attribute in the hierarchy
                        if (searchTree.HasChild(consolidatedResult.Attribute.PeoplePickerAttributeHierarchyNodeId))
                        {
                            matchNode = searchTree.Children.First(
                                a => a.HierarchyNodeID == consolidatedResult.Attribute.PeoplePickerAttributeHierarchyNodeId);
                        }
                        else
                        {
                            matchNode = new SPProviderHierarchyNode(
                                ProviderInternalName, 
                                consolidatedResult.Attribute.PeoplePickerAttributeDisplayName, 
                                consolidatedResult.Attribute.PeoplePickerAttributeHierarchyNodeId, 
                                true);
                            searchTree.AddChild(matchNode);
                        }

                        matchNode.AddEntity(consolidatedResult.PickerEntity);
                    }
                }
            });
        }

        protected void Initialize()
        {
            this.associatedSPTrustedLoginProvider = Helper.GetSPTrustAssociatedWithCP(ProviderInternalName);
            if (this.associatedSPTrustedLoginProvider != null)
            {
                this.auth0Config = Auth0Config.GetDefaultSettings();

                this.attributesDefinitionList = this.auth0Config.AttributesList;
                this.PopulateActualAttributesList();
            }
        }

        protected virtual void ResolveInputBulk(string input, IEnumerable<ClaimAttribute> attributesToQuery, bool exactSearch)
        {
            // TODO: perform search with Auth0 API
            //var client = new Auth0.Client(
            //    this.auth0Config.ClientId,
            //    this.auth0Config.ClientSecret,
            //    this.auth0Config.Domain);

            this.consolidatedResults = new List<ConsolidatedResult>();
        }

        private void PopulateActualAttributesList()
        {
            if (this.associatedSPTrustedLoginProvider == null)
            {
                return;
            }

            // Get attributes defined in trust based on their claim type (unique way to map them)
            var attributesDefinedInTrust = new Collection<ClaimAttribute>();
            foreach (var attr in this.attributesDefinitionList.Where(a => this.associatedSPTrustedLoginProvider.ClaimTypes.Contains(a.ClaimType)))
            {
                attributesDefinedInTrust.Add(attr);
            }

            // Make sure that the identity claim is in this collection
            if (attributesDefinedInTrust == null || attributesDefinedInTrust.Count(a => a.ClaimType == this.associatedSPTrustedLoginProvider.IdentityClaimTypeInformation.MappedClaimType) == 0)
            {
                // string.Format("[{0}] Impossible to continue because identity claim \"{1}\" is missing in the list of attributes to query. Please use method PopulateAttributesDefinition() to add it", ProviderInternalName, AssociatedSPTrustedLoginProvider.IdentityClaimTypeInformation.MappedClaimType), TraceSeverity.Unexpected, EventSeverity.ErrorCritical);
                return;
            }

            // Check if attributes that should be always used are in the list, and add them if not
            var additionalAttributes = new Collection<ClaimAttribute>();
            foreach (var attr in this.attributesDefinitionList.Where(
                a => a.ResolveAsIdentityClaim == true && !attributesDefinedInTrust.Any(x => x.ClaimType == a.ClaimType)))
            {
                attr.ClaimType = this.associatedSPTrustedLoginProvider.IdentityClaimTypeInformation.MappedClaimType;
                attr.ClaimEntityType = SPClaimEntityTypes.User;
                additionalAttributes.Add(attr);
            }

            this.attributesToQuery = attributesDefinedInTrust.Union(additionalAttributes);

            // Parse each attribute to configure its settings from the corresponding claim types defined in the SPTrustedLoginProvider
            foreach (var attr in this.attributesToQuery.Where(a => a.ClaimType != null))
            {
                var trustedClaim = this.associatedSPTrustedLoginProvider.GetClaimTypeInformationFromMappedClaimType(attr.ClaimType);
                if (trustedClaim == null)
                {
                    continue;
                }

                attr.PeoplePickerAttributeDisplayName = trustedClaim.DisplayName;
                attr.PeoplePickerAttributeHierarchyNodeId = trustedClaim.InputClaimType;
            }

            // Get identity attribute from SPTrustedLoginProvider configuration
            this.identityAttribute = this.attributesToQuery.Where(
                a => a.ClaimType == this.associatedSPTrustedLoginProvider.IdentityClaimTypeInformation.MappedClaimType).First();
        }
    }
}