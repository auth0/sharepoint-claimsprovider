namespace Auth0.ClaimsProvider
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Net;
    using Auth0.ClaimsProvider.Configuration;
    using Microsoft.SharePoint;
    using Microsoft.SharePoint.Administration.Claims;
    using Microsoft.SharePoint.WebControls;

    public class CustomClaimsProvider : SPClaimProvider
    {
        private const string SocialHierarchyNode = "Social";
        private const string EnterpriseHierarchyNode = "Enterprise";

        private readonly IConfigurationRepository configurationRepository;

        private SPTrustedLoginProvider associatedSPTrustedLoginProvider; // Name of the SPTrustedLoginProvider associated with the claim provider
        private Auth0.Client auth0Client;
        private Auth0Config auth0Config;
        private ClaimAttribute identityAttribute; // Attribute mapped to the identity claim in the SPTrustedLoginProvider
        private ICollection<ConsolidatedResult> consolidatedResults;
        private bool alwaysResolveValue;
        private string pickerEntityGroupName;

        public CustomClaimsProvider(string displayName)
            : this(displayName, new ConfigurationRepository())
        { 
        }

        public CustomClaimsProvider(string displayName, IConfigurationRepository configurationRepository)
            : base(displayName)
        {
            if (SPContext.Current == null)
            {
                return;
            }

            this.configurationRepository = configurationRepository;

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
            get { return "[{0}] {1} = {2}"; }
        }

        protected virtual string PickerEntityOnMouseOverAdditionalAttribute
        {
            get { return "[{0}] {1} ({2} = {3})"; }
        }

        protected override void FillClaimTypes(List<string> claimTypes)
        {
            if (claimTypes == null)
            {
                throw new ArgumentNullException("claimTypes");
            }

            if (this.identityAttribute == null)
            {
                return;
            }

            claimTypes.Add(this.identityAttribute.ClaimType);
        }

        protected override void FillClaimValueTypes(List<string> claimValueTypes)
        {
            if (claimValueTypes == null)
            {
                throw new ArgumentNullException("claimValueTypes");
            }

            if (this.identityAttribute == null)
            {
                return;
            }

            claimValueTypes.Add(this.identityAttribute.ClaimValueType);
        }

        protected override void FillClaimsForEntity(Uri context, SPClaim entity, List<SPClaim> claims)
        {
            throw new NotImplementedException();
        }

        protected override void FillEntityTypes(List<string> entityTypes)
        {
            if (this.identityAttribute == null)
            {
                return;
            }

            entityTypes.Add(this.identityAttribute.ClaimEntityType);
        }

        protected override void FillHierarchy(Uri context, string[] entityTypes, string hierarchyNodeID, int numberOfLevels, SPProviderHierarchyTree hierarchy)
        {
            if (this.identityAttribute == null)
            {
                return;
            }

            if (hierarchyNodeID == null)
            {
                this.CreateConnectionsNodes(hierarchy);
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

            if (this.identityAttribute == null)
            {
                return;
            }

            SPSecurity.RunWithElevatedPrivileges(delegate
            {
                this.Initialize();

                this.ResolveInputBulk(resolveInput.Value, string.Empty, true);
                
                if (this.consolidatedResults != null && this.consolidatedResults.Count > 0)
                {
                    resolved.Add(this.consolidatedResults.ElementAt(0).PickerEntity);
                    return;
                }

                if (this.alwaysResolveValue)
                {
                    // TODO
                }
            });
        }

        protected override void FillResolve(Uri context, string[] entityTypes, string resolveInput, List<PickerEntity> resolved)
        {
            if (this.identityAttribute == null)
            {
                return;
            }

            SPSecurity.RunWithElevatedPrivileges(delegate
            {
                this.Initialize();

                this.ResolveInputBulk(resolveInput, string.Empty, false);

                if (this.consolidatedResults != null)
                {
                    foreach (var result in this.consolidatedResults)
                    {
                        resolved.Add(result.PickerEntity);
                    }
                }

                if (this.alwaysResolveValue)
                {
                    // TODO
                }
            });
        }

        protected override void FillSchema(SPProviderSchema schema)
        {
        }

        protected override void FillSearch(Uri context, string[] entityTypes, string searchPattern, string hierarchyNodeID, int maxCount, SPProviderHierarchyTree searchTree)
        {
            if (this.identityAttribute == null)
            {
                return;
            }

            SPProviderHierarchyNode matchNode = null;
            SPSecurity.RunWithElevatedPrivileges(delegate
            {
                this.Initialize();

                this.ResolveInputBulk(searchPattern, hierarchyNodeID, false);

                if (this.consolidatedResults != null)
                {
                    this.CreateConnectionsNodes(searchTree);

                    foreach (var consolidatedResult in this.consolidatedResults)
                    {
                        // Add current PickerEntity to the corresponding attribute in the hierarchy
                        var connectionNode = consolidatedResult.Auth0User.Identities.First().IsSocial ?
                            searchTree.Children.First(c => c.HierarchyNodeID == SocialHierarchyNode.ToLowerInvariant()) :
                            searchTree.Children.First(c => c.HierarchyNodeID == EnterpriseHierarchyNode.ToLowerInvariant());

                        if (connectionNode.HasChild(consolidatedResult.Attribute.PeoplePickerAttributeHierarchyNodeId))
                        {
                            matchNode = connectionNode.Children.First(
                                c => c.HierarchyNodeID == consolidatedResult.Attribute.PeoplePickerAttributeHierarchyNodeId);
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

                if (this.alwaysResolveValue)
                {
                    // TODO
                }
            });
        }

        protected void Initialize()
        {
            this.associatedSPTrustedLoginProvider = Helper.GetSPTrustAssociatedWithCP(ProviderInternalName);
            if (this.associatedSPTrustedLoginProvider != null)
            {
                this.auth0Config = this.configurationRepository.GetConfiguration();

                // TODO: validate clientId, clientSecret and domain
                this.auth0Client = new Auth0.Client(
                    this.auth0Config.ClientId,
                    this.auth0Config.ClientSecret,
                    this.auth0Config.Domain);

                this.alwaysResolveValue = this.auth0Config.AlwaysResolveUserInput;
                this.pickerEntityGroupName = this.auth0Config.PickerEntityGroupName;
                this.identityAttribute = this.auth0Config.AttributesToShow.Where(
                    a => a.ClaimType == this.associatedSPTrustedLoginProvider.IdentityClaimTypeInformation.MappedClaimType).First();
            }
        }

        protected virtual void ResolveInputBulk(string input, string connectionName, bool exactSearch)
        {
            this.consolidatedResults = new Collection<ConsolidatedResult>();

            if (string.IsNullOrEmpty(input) || this.identityAttribute == null)
            {
                return;
            }

            IEnumerable<Auth0.User> users;

            if (!string.IsNullOrEmpty(connectionName))
            {
                users = this.auth0Client.GetUsersByConnection(connectionName, input);
            }
            else 
            {
                var socialUsers = this.auth0Client.GetSocialUsers(input);
                var enterpriseUsers = this.auth0Client.GetEnterpriseUsers(input);

                users = socialUsers.Union(enterpriseUsers);
            }

            foreach (var user in users)
            {
                var pickerAttributeName = string.IsNullOrEmpty(connectionName) ?
                    user.Identities.First().Connection : connectionName;

                var claimAttribute = new ClaimAttribute
                {
                    Auth0AttributeName = this.identityAttribute.Auth0AttributeName,
                    ClaimEntityType = this.identityAttribute.ClaimEntityType,
                    ClaimType = this.identityAttribute.ClaimType,
                    ClaimValueType = this.identityAttribute.ClaimValueType,
                    PeopleEditorEntityDataKey = this.identityAttribute.PeopleEditorEntityDataKey,
                    PeoplePickerAttributeDisplayName = pickerAttributeName,
                    PeoplePickerAttributeHierarchyNodeId = pickerAttributeName
                };

                this.consolidatedResults.Add(new ConsolidatedResult
                {
                    Attribute = claimAttribute,
                    Auth0User = user,
                    PickerEntity = this.GetPickerEntity(claimAttribute, user)
                });
            }
        }

        protected virtual PickerEntity GetPickerEntity(ClaimAttribute attribute, Auth0.User auth0User)
        {
            var claim = new SPClaim(
                    this.identityAttribute.ClaimType,
                    Helper.GetPropertyValue(auth0User, this.identityAttribute.Auth0AttributeName).ToString(),
                    this.identityAttribute.ClaimValueType,
                    SPOriginalIssuers.Format(SPOriginalIssuerType.TrustedProvider, this.associatedSPTrustedLoginProvider.Name));

            PickerEntity pe = CreatePickerEntity();
            pe.DisplayText = Helper.GetPropertyValue(auth0User, this.identityAttribute.Auth0AttributeName).ToString();
            pe.EntityType = this.identityAttribute.ClaimEntityType;
            pe.Description = string.Format(
                this.PickerEntityOnMouseOverAdditionalAttribute,
                ProviderInternalName,
                Helper.GetPropertyValue(auth0User, this.identityAttribute.Auth0AttributeName).ToString(),
                attribute.Auth0AttributeName,
                Helper.GetPropertyValue(auth0User, attribute.Auth0AttributeName).ToString());

            pe.Claim = claim;
            pe.IsResolved = true;
            pe.EntityGroupName = this.pickerEntityGroupName;

            return pe;
        }

        private static SPProviderHierarchyNode GetParentNode(string nodeName)
        {
            return new Microsoft.SharePoint.WebControls.SPProviderHierarchyNode(
                ProviderInternalName,
                nodeName,
                nodeName.ToLowerInvariant(),
                false);
        }

        private void CreateConnectionsNodes(SPProviderHierarchyTree hierarchy)
        {
            // Enterprise nodes
            var enterpriseNode = GetParentNode(EnterpriseHierarchyNode);
            foreach (var connection in this.auth0Client.GetEnterpriseConnections().Where(c => c.Enabled))
            {
                enterpriseNode.AddChild(
                    new Microsoft.SharePoint.WebControls.SPProviderHierarchyNode(
                        ProviderInternalName,
                        connection.Name,
                        connection.Name,
                        true));
            }

            hierarchy.AddChild(enterpriseNode);

            // Social nodes
            var socialNode = GetParentNode(SocialHierarchyNode);
            foreach (var connection in this.auth0Client.GetSocialConnections().Where(c => c.Enabled))
            {
                socialNode.AddChild(
                    new Microsoft.SharePoint.WebControls.SPProviderHierarchyNode(
                        ProviderInternalName,
                        connection.Name,
                        connection.Name,
                        true));
            }

            hierarchy.AddChild(socialNode);
        }
    }
}