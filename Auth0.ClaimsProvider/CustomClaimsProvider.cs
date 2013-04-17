namespace Auth0.ClaimsProvider
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Linq.Dynamic;
    using System.Net;
    using Microsoft.IdentityModel.Claims;
    using Microsoft.SharePoint;
    using Microsoft.SharePoint.Administration.Claims;
    using Microsoft.SharePoint.WebControls;

    public class CustomClaimsProvider : SPClaimProvider
    {
        private SPTrustedLoginProvider associatedSPTrustedLoginProvider; // Name of the SPTrustedLoginProvider associated with the claim provider
        private Auth0.Client auth0Client;
        private Auth0Config auth0Config;
        private ClaimAttribute identityAttribute; // Attribute mapped to the identity claim in the SPTrustedLoginProvider
        private IEnumerable<ClaimAttribute> attributesToQuery;
        private IEnumerable<ClaimAttribute> attributesDefinitionList;
        private ICollection<ConsolidatedResult> consolidatedResults;
        private IdentityValueDisplay identityValueDisplay;
        private ClaimAttribute auth0ValueToDisplayForIdentityAttribute;
        private bool alwaysResolveValue;

        public CustomClaimsProvider(string displayName)
            : base(displayName)
        {
            if (SPContext.Current == null)
            {
                return;
            }

            // TODO: remove this
            ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };

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

        internal string Auth0ConnectionName
        {
            get 
            {
                var claimsIdentity = System.Threading.Thread.CurrentPrincipal.Identity as ClaimsIdentity;
                if (claimsIdentity != null)
                {
                    return claimsIdentity.Claims.Any(c => c.ClaimType == this.auth0Config.ConnectionClaimType) ?
                        claimsIdentity.Claims.First(c => c.ClaimType == this.auth0Config.ConnectionClaimType).Value :
                        string.Empty;
                }

                return string.Empty; 
            }
        }

        protected virtual string PickerEntityDisplayText
        {
            get { return "({0}) {1}"; }
        }

        protected virtual string PickerEntityDisplayTextAdditionalAttribute
        {
            get { return "{0} ({1} = {2})"; }
        }

        protected virtual string PickerEntityOnMouseOver
        {
            get { return "[{0}] {1}={2}"; }
        }

        protected virtual string PickerEntityOnMouseOverAdditionalAttribute
        {
            get { return "[{0}] {1} ({2} = {3})"; }
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
            if (this.attributesToQuery == null)
            {
                return;
            }

            if (hierarchyNodeID == null)
            {
                // First load
                foreach (var connection in this.auth0Client.GetConnections())
                {
                    hierarchy.AddChild(
                        new Microsoft.SharePoint.WebControls.SPProviderHierarchyNode(
                            ProviderInternalName,
                            connection.Name,
                            connection.Name, // hierarchyNodeID
                            true));
                }
            }
        }

        protected override void FillResolve(Uri context, string[] entityTypes, SPClaim resolveInput, List<PickerEntity> resolved)
        {
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

                // Resolve value only against the incoming claim type that uniquely identifies the user (mail, sAMAccountName)
                var attributes = this.attributesToQuery.Where(a => a.ClaimType == resolveInput.ClaimType && !a.ResolveAsIdentityClaim);
                if (attributes.Count() != 1)
                {
                    // Should always find only 1 attribute at this stage
                    return;
                }

                this.ResolveInputBulk(resolveInput.Value, attributes, true);
                if (this.consolidatedResults != null && this.consolidatedResults.Count > 0)
                {
                    resolved.Add(this.consolidatedResults.ElementAt(0).PickerEntity);
                    return;
                }

                if (this.alwaysResolveValue)
                {
                    Collection<PickerEntity> entities = this.CreatePickerEntityForEachClaimType(resolveInput.Value);
                    if (entities != null)
                    {
                        resolved.Add(entities.Where(x => x.Claim.ClaimType == resolveInput.ClaimType).FirstOrDefault());
                    }
                }
            });
        }

        protected override void FillResolve(Uri context, string[] entityTypes, string resolveInput, List<PickerEntity> resolved)
        {
            if (this.attributesToQuery == null)
            {
                return;
            }

            string input = resolveInput;
            SPSecurity.RunWithElevatedPrivileges(delegate
            {
                this.Initialize();

                var attributeCollection = this.attributesToQuery.Where(a => entityTypes.Contains(a.ClaimEntityType) && !a.ResolveAsIdentityClaim);
                this.ResolveInputBulk(input, attributeCollection, false);

                if (this.consolidatedResults != null)
                {
                    foreach (var result in this.consolidatedResults)
                    {
                        resolved.Add(result.PickerEntity);
                    }
                }

                if (this.alwaysResolveValue)
                {
                    Collection<PickerEntity> entities = this.CreatePickerEntityForEachClaimType(input);
                    if (entities != null)
                    {
                        foreach (var entity in entities)
                        {
                            resolved.Add(entity);
                        }
                    }
                }
            });
        }

        protected override void FillSchema(SPProviderSchema schema)
        {
        }

        protected override void FillSearch(Uri context, string[] entityTypes, string searchPattern, string hierarchyNodeID, int maxCount, SPProviderHierarchyTree searchTree)
        {
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

                if (this.consolidatedResults != null)
                {
                    foreach (var consolidatedResult in this.consolidatedResults)
                    {
                        // Add current PickerEntity to the corresponding attribute in the hierarchy
                        if (searchTree.HasChild(consolidatedResult.Attribute.PeoplePickerAttributeHierarchyNodeId))
                        {
                            matchNode = searchTree.Children.First(
                                c => c.HierarchyNodeID == consolidatedResult.Attribute.PeoplePickerAttributeHierarchyNodeId);
                        }
                        else
                        {
                            matchNode = new SPProviderHierarchyNode(ProviderInternalName, consolidatedResult.Attribute.PeoplePickerAttributeDisplayName, consolidatedResult.Attribute.PeoplePickerAttributeHierarchyNodeId, true);
                            searchTree.AddChild(matchNode);
                        }

                        matchNode.AddEntity(consolidatedResult.PickerEntity);
                    }
                }

                if (this.alwaysResolveValue)
                {
                    Collection<PickerEntity> entities = this.CreatePickerEntityForEachClaimType(searchPattern);
                    if (entities != null)
                    {
                        foreach (var entity in entities)
                        {
                            // Add current PickerEntity to the corresponding attribute in the hierarchy
                            var hirarchyDetails = from a in this.attributesToQuery
                                                  where a.ClaimType == entity.Claim.ClaimType && !a.ResolveAsIdentityClaim
                                                  select new 
                                                  { 
                                                      HierarchyNodeId = a.PeoplePickerAttributeHierarchyNodeId, 
                                                      HierarchyNodeDisplayName = a.PeoplePickerAttributeDisplayName 
                                                  };

                            if (searchTree.HasChild(hirarchyDetails.FirstOrDefault().HierarchyNodeId))
                            {
                                matchNode = searchTree.Children.First(c => c.HierarchyNodeID == hirarchyDetails.FirstOrDefault().HierarchyNodeId);
                            }
                            else
                            {
                                matchNode = new SPProviderHierarchyNode(ProviderInternalName, hirarchyDetails.FirstOrDefault().HierarchyNodeDisplayName, hirarchyDetails.FirstOrDefault().HierarchyNodeId, true);
                                searchTree.AddChild(matchNode);
                            }

                            matchNode.AddEntity(entity);
                        }
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

                // TODO: validate clientId, clientSecret and domain
                this.auth0Client = new Auth0.Client(
                    this.auth0Config.ClientId,
                    this.auth0Config.ClientSecret,
                    this.auth0Config.Domain);

                this.alwaysResolveValue = this.auth0Config.AlwaysResolveUserInput;
                this.identityValueDisplay = IdentityValueDisplay.IdentityValue;
                this.attributesDefinitionList = this.auth0Config.AttributesList;
                this.PopulateActualAttributesList();
            }
        }

        protected virtual void ResolveInputBulk(string input, IEnumerable<ClaimAttribute> attributesToQuery, bool exactSearch)
        {
            this.consolidatedResults = new Collection<ConsolidatedResult>();

            if (string.IsNullOrEmpty(input) || attributesToQuery == null || attributesToQuery.Count() == 0)
            {
                return;
            }

            var connectionName = this.Auth0ConnectionName;
            if (!string.IsNullOrEmpty(connectionName))
            {
                var users = this.auth0Client.GetUsersByConnection(connectionName);
                if (users != null && users.Count() > 0)
                {
                    foreach (var attributeToQuery in attributesToQuery)
                    {
                        var filter = attributeToQuery.Auth0AttributeName;
                        var query = filter + (exactSearch ? ".Equals(@0, @1)" : ".IndexOf(@0, @1) > -1");

                        attributeToQuery.PeoplePickerAttributeDisplayName = connectionName;
                        attributeToQuery.PeoplePickerAttributeHierarchyNodeId = connectionName;

                        try
                        {
                            var filteredUsers = users.AsQueryable()
                                                     .Where(query, input, StringComparison.OrdinalIgnoreCase)
                                                     .Select(u => new KeyValuePair<Auth0.User, string>(u, Helper.GetPropertyValue(u, filter).ToString()));
                            
                            foreach (var user in filteredUsers)
                            {
                                this.consolidatedResults.Add(new ConsolidatedResult
                                {
                                    Attribute = attributeToQuery,
                                    Auth0User = user.Key
                                });
                            }
                        }
                        catch (ParseException)
                        { 
                            // Invalid filter
                        }
                    }

                    foreach (var consolidatedResult in this.consolidatedResults)
                    {
                        consolidatedResult.PickerEntity = this.GetPickerEntity(consolidatedResult);
                    }
                }
            }
        }

        protected virtual PickerEntity GetPickerEntity(ConsolidatedResult result)
        {
            PickerEntity pe = CreatePickerEntity();
            SPClaim claim;

            if (result.Attribute.ResolveAsIdentityClaim)
            {
                claim = new SPClaim(
                    this.identityAttribute.ClaimType,
                    Helper.GetPropertyValue(result.Auth0User, this.identityAttribute.Auth0AttributeName).ToString(),
                    this.identityAttribute.ClaimValueType,
                    SPOriginalIssuers.Format(SPOriginalIssuerType.TrustedProvider, this.associatedSPTrustedLoginProvider.Name));

                var displayText = string.Empty;
                switch (this.identityValueDisplay)
                {
                    case IdentityValueDisplay.IdentityValue:
                        displayText = Helper.GetPropertyValue(result.Auth0User, this.identityAttribute.Auth0AttributeName).ToString();
                        break;

                    case IdentityValueDisplay.SpecificValue:
                        this.auth0ValueToDisplayForIdentityAttribute = this.attributesToQuery.Where(x => x.Auth0AttributeName == this.auth0Config.Auth0ValueToDisplayForIdentityAttribute).FirstOrDefault();
                        if (this.auth0ValueToDisplayForIdentityAttribute != null && !string.IsNullOrEmpty(Helper.GetPropertyValue(result.Auth0User, this.auth0ValueToDisplayForIdentityAttribute.Auth0AttributeName).ToString()))
                        {
                            displayText = Helper.GetPropertyValue(result.Auth0User, this.auth0ValueToDisplayForIdentityAttribute.Auth0AttributeName).ToString();
                        }
                        else
                        {
                            displayText = Helper.GetPropertyValue(result.Auth0User, this.identityAttribute.Auth0AttributeName).ToString();
                        }

                        break;

                    case IdentityValueDisplay.IncludeValueThatResolvedInput:
                        displayText = string.Format(
                            this.PickerEntityDisplayTextAdditionalAttribute,
                            Helper.GetPropertyValue(result.Auth0User, this.identityAttribute.Auth0AttributeName).ToString(),
                            result.Attribute.Auth0AttributeName,
                            Helper.GetPropertyValue(result.Auth0User, result.Attribute.Auth0AttributeName).ToString());
                        break;

                    default:
                        displayText = Helper.GetPropertyValue(result.Auth0User, this.identityAttribute.Auth0AttributeName).ToString();
                        break;
                }

                pe.DisplayText = displayText;
                pe.EntityType = this.identityAttribute.ClaimEntityType;
                pe.Description = string.Format(
                    this.PickerEntityOnMouseOverAdditionalAttribute,
                    ProviderInternalName,
                    Helper.GetPropertyValue(result.Auth0User, this.identityAttribute.Auth0AttributeName).ToString(),
                    result.Attribute.Auth0AttributeName,
                    Helper.GetPropertyValue(result.Auth0User, result.Attribute.Auth0AttributeName).ToString());
            }
            else
            {
                claim = new SPClaim(
                    result.Attribute.ClaimType,
                    Helper.GetPropertyValue(result.Auth0User, result.Attribute.Auth0AttributeName).ToString(),
                    result.Attribute.ClaimValueType,
                    SPOriginalIssuers.Format(SPOriginalIssuerType.TrustedProvider, this.associatedSPTrustedLoginProvider.Name));

                // Display value only when the claim type is the identity claim, so that Welcome menu shows only value
                // For other claim types: Display the claim type to easily identify on what attribute is defined the value
                var displayText = string.Empty;
                if (this.associatedSPTrustedLoginProvider.IdentityClaimTypeInformation.InputClaimType == claim.ClaimType)
                {
                    // Identity claim type may use an attribute where its value doesn't mean anything to users (for example a corporate ID)
                    // In that case it is possible to use another Auth0 attrbute to display the permission
                    // So that user permissions list is more readable
                    if (this.identityValueDisplay == IdentityValueDisplay.SpecificValue && this.auth0ValueToDisplayForIdentityAttribute != null && !string.IsNullOrEmpty(Helper.GetPropertyValue(result.Auth0User, this.auth0ValueToDisplayForIdentityAttribute.Auth0AttributeName).ToString()))
                    {
                        displayText = Helper.GetPropertyValue(result.Auth0User, this.auth0ValueToDisplayForIdentityAttribute.Auth0AttributeName).ToString();
                    }
                    else
                    {
                        displayText = Helper.GetPropertyValue(result.Auth0User, result.Attribute.Auth0AttributeName).ToString();
                    }
                }
                else
                {
                    displayText = string.Format(
                        this.PickerEntityDisplayText,
                        result.Attribute.PeoplePickerAttributeDisplayName,
                        Helper.GetPropertyValue(result.Auth0User, result.Attribute.Auth0AttributeName).ToString());
                }

                pe.DisplayText = displayText;
                pe.EntityType = result.Attribute.ClaimEntityType;
                pe.Description = string.Format(
                    this.PickerEntityOnMouseOver,
                    ProviderInternalName,
                    result.Attribute.Auth0AttributeName,
                    Helper.GetPropertyValue(result.Auth0User, result.Attribute.Auth0AttributeName).ToString());
            }

            pe.Claim = claim;
            pe.IsResolved = true;
            pe.EntityGroupName = "Results";

            if (result.Attribute.ClaimEntityType == SPClaimEntityTypes.User)
            {
                // Try to fill some properties in the hashtable of the PickerEntry based on the Auth0 object
                // so that the picker entity is populated with as many attributes as possible
                var entityAttribs = from a in this.attributesToQuery
                                    where !string.IsNullOrEmpty(a.PeopleEditorEntityDataKey)
                                    select new { Auth0AttributeName = a.Auth0AttributeName, PeopleEditorEntityDataKey = a.PeopleEditorEntityDataKey };

                foreach (var entityAttrib in entityAttribs)
                {
                    pe.EntityData[entityAttrib.PeopleEditorEntityDataKey] =
                        Helper.GetPropertyValue(result.Auth0User, entityAttrib.Auth0AttributeName).ToString();
                }
            }

            return pe;
        }

        /// <summary>
        /// Create a PickerEntity of the user input for each of the claim type registered in the trust
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        protected virtual Collection<PickerEntity> CreatePickerEntityForEachClaimType(string input)
        {
            var attributeCollection = this.attributesToQuery.Where(a => !a.ResolveAsIdentityClaim);
            var entities = new Collection<PickerEntity>();

            foreach (var attribute in attributeCollection)
            {
                PickerEntity pe = CreatePickerEntity();
                var claim = new SPClaim(
                    attribute.ClaimType,
                    input,
                    attribute.ClaimValueType,
                    SPOriginalIssuers.Format(SPOriginalIssuerType.TrustedProvider, this.associatedSPTrustedLoginProvider.Name));

                // Display value only when the claim type is the identity claim, so that Welcome menu shows only value
                // For other claim types: Display the claim type to easily identify on what attribute is defined the value
                if (this.associatedSPTrustedLoginProvider.IdentityClaimTypeInformation.InputClaimType == claim.ClaimType)
                {
                    pe.DisplayText = input;
                }
                else
                {
                    pe.DisplayText = string.Format(
                        this.PickerEntityDisplayText,
                        attribute.PeoplePickerAttributeDisplayName,
                        input);
                }

                pe.EntityType = attribute.ClaimEntityType;
                pe.Description = string.Format(
                    this.PickerEntityOnMouseOver,
                    ProviderInternalName,
                    attribute.Auth0AttributeName,
                    input);

                pe.Claim = claim;
                pe.IsResolved = true;
                pe.EntityGroupName = "Results";

                entities.Add(pe);
            }

            return entities;
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
            if (attributesDefinedInTrust == null || attributesDefinedInTrust.Count(
                a => a.ClaimType == this.associatedSPTrustedLoginProvider.IdentityClaimTypeInformation.MappedClaimType) == 0)
            {
                // string.Format("[{0}] Impossible to continue because identity claim \"{1}\" is missing in the list of attributes to query. Please use method PopulateAttributesDefinition() to add it", ProviderInternalName, AssociatedSPTrustedLoginProvider.IdentityClaimTypeInformation.MappedClaimType)
                return;
            }

            // Check if attributes that should be always used (AttributeUsage.AlwaysSearchAgainstLDAP) are in the list, and add them if not
            var additionalAttributes = new Collection<ClaimAttribute>();
            foreach (var attr in this.attributesDefinitionList.Where(
                a => a.ResolveAsIdentityClaim == true && !attributesDefinedInTrust.Any(at => at.Auth0AttributeName == a.Auth0AttributeName)))
            {
                attr.ClaimType = this.associatedSPTrustedLoginProvider.IdentityClaimTypeInformation.MappedClaimType;
                attr.ClaimEntityType = SPClaimEntityTypes.User;
                additionalAttributes.Add(attr);
            }

            this.attributesToQuery = attributesDefinedInTrust.Union(additionalAttributes);

            // Get identity attribute from SPTrustedLoginProvider configuration
            this.identityAttribute = this.attributesToQuery.Where(
                a => a.ClaimType == this.associatedSPTrustedLoginProvider.IdentityClaimTypeInformation.MappedClaimType).First();
        }
    }
}