namespace Auth0.ClaimsProvider
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Net;
    using Auth0.ClaimsProvider.Configuration;
    using Microsoft.SharePoint;
    using Microsoft.SharePoint.Administration;
    using Microsoft.SharePoint.Administration.Claims;
    using Microsoft.SharePoint.WebControls;

    public class CustomClaimsProvider : SPClaimProvider
    {
        private const string SocialHierarchyNode = "Social";
        private const string EnterpriseHierarchyNode = "Enterprise";
        private const string IdentifierClaimsType = "http://schemas.auth0.com/connection_email";
        private const string ClientIdClaimsType = "http://schemas.auth0.com/clientID";

        private readonly IConfigurationRepository configurationRepository;

        private SPTrustedLoginProvider associatedSPTrustedLoginProvider; // Name of the SPTrustedLoginProvider associated with the claim provider
        private Auth0.Client auth0Client;
        private Auth0Config auth0Config;
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
            get { return "{0} ({1})"; }
        }

        protected virtual string PickerEntityOnMouseOver
        {
            get { return "[{0}] {1} ({2} = {3})"; }
        }

        protected override void FillClaimTypes(List<string> claimTypes)
        {
            if (claimTypes == null)
            {
                throw new ArgumentNullException("claimTypes");
            }

            claimTypes.Add(IdentifierClaimsType);
        }

        protected override void FillClaimValueTypes(List<string> claimValueTypes)
        {
            if (claimValueTypes == null)
            {
                throw new ArgumentNullException("claimValueTypes");
            }

            claimValueTypes.Add(Microsoft.IdentityModel.Claims.ClaimValueTypes.String);
        }

        protected override void FillClaimsForEntity(Uri context, SPClaim entity, List<SPClaim> claims)
        {
            throw new NotImplementedException();
        }

        protected override void FillEntityTypes(List<string> entityTypes)
        {
            entityTypes.Add(SPClaimEntityTypes.User);
        }

        protected override void FillHierarchy(Uri context, string[] entityTypes, string hierarchyNodeID, int numberOfLevels, SPProviderHierarchyTree hierarchy)
        {
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

            SPSecurity.RunWithElevatedPrivileges(delegate
            {
                var input = resolveInput.Value.Contains('|') ?
                    resolveInput.Value.Split('|')[1] : resolveInput.Value;
                var connectionName = resolveInput.Value.Contains('|') ?
                    resolveInput.Value.Split('|')[0] : string.Empty;

                this.ResolveInputBulk(input, connectionName, true);
                
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
            SPSecurity.RunWithElevatedPrivileges(delegate
            {
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
            SPProviderHierarchyNode matchNode = null;
            SPSecurity.RunWithElevatedPrivileges(delegate
            {
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
            this.associatedSPTrustedLoginProvider = Utils.GetSPTrustAssociatedWithCP(ProviderInternalName);
            if (this.associatedSPTrustedLoginProvider != null)
            {
                this.auth0Config = this.configurationRepository.GetConfiguration();

                try
                {
                    var clientsIds = this.auth0Config.ClientId.Split(',');
                    var clientsSecrets = this.auth0Config.ClientSecret.Split(',');
                    var clientIdIndex = Array.IndexOf(clientsIds, Utils.GetClaimsValue(ClientIdClaimsType));

                    this.auth0Client = new Auth0.Client(
                        clientsIds[clientIdIndex],
                        clientsSecrets[clientIdIndex],
                        this.auth0Config.Domain);
                }
                catch (Exception ex)
                {
                    Utils.LogToULS(ex.ToString(), TraceSeverity.Unexpected, EventSeverity.Error);
                }

                this.alwaysResolveValue = this.auth0Config.AlwaysResolveUserInput;
                this.pickerEntityGroupName = this.auth0Config.PickerEntityGroupName;
            }
        }

        protected virtual void ResolveInputBulk(string input, string connectionName, bool exactSearch)
        {
            this.consolidatedResults = new Collection<ConsolidatedResult>();

            if (string.IsNullOrEmpty(input))
            {
                return;
            }

            IEnumerable<Auth0.User> users = null;

            try
            {
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
            }
            catch (Exception ex)
            {
                Utils.LogToULS(ex.ToString(), TraceSeverity.Unexpected, EventSeverity.Error);
            }

            if (users != null)
            {
                foreach (var user in users)
                {
                    var pickerAttributeName = string.IsNullOrEmpty(connectionName) ?
                        user.Identities.First().Connection : connectionName;

                    var claimAttribute = new ClaimAttribute
                    {
                        ClaimEntityType = SPClaimEntityTypes.User,
                        PeoplePickerAttributeDisplayName = pickerAttributeName,
                        PeoplePickerAttributeHierarchyNodeId = pickerAttributeName
                    };

                    this.consolidatedResults.Add(new ConsolidatedResult
                    {
                        Attribute = claimAttribute,
                        Auth0User = user,
                        PickerEntity = this.GetPickerEntity(user)
                    });
                }
            }
        }

        protected virtual PickerEntity GetPickerEntity(Auth0.User auth0User)
        {
            var claim = new SPClaim(
                    IdentifierClaimsType,
                    auth0User.UniqueEmail(),
                    Microsoft.IdentityModel.Claims.ClaimValueTypes.String,
                    SPOriginalIssuers.Format(SPOriginalIssuerType.TrustedProvider, this.associatedSPTrustedLoginProvider.Name));

            PickerEntity pe = CreatePickerEntity();
            pe.EntityType = SPClaimEntityTypes.User;

            pe.DisplayText = string.Format(this.PickerEntityDisplayText, auth0User.Name, auth0User.Email);
            pe.Description = string.Format(
                this.PickerEntityOnMouseOver,
                ProviderInternalName + " | " + auth0User.Identities.First().Connection,
                auth0User.Email,
                "Name",
                auth0User.Name);

            pe.Claim = claim;
            pe.IsResolved = true;
            pe.EntityGroupName = this.pickerEntityGroupName;

            pe.EntityData[PeopleEditorEntityDataKeys.DisplayName] = auth0User.Name;
            pe.EntityData[PeopleEditorEntityDataKeys.Email] = auth0User.Email;
            pe.EntityData["Picture"] = auth0User.Picture;

            return pe;
        }

        private static SPProviderHierarchyNode GetParentNode(string nodeName)
        {
            return new SPProviderHierarchyNode(
                ProviderInternalName,
                nodeName,
                nodeName.ToLowerInvariant(),
                false);
        }

        private static void CreateConnectionNodes(SPProviderHierarchyTree hierarchy, string connectionType, IEnumerable<Connection> connections)
        {
            var parentNode = GetParentNode(connectionType);
            if (connections != null)
            {
                foreach (var connection in connections.Where(c => c.Enabled))
                {
                    parentNode.AddChild(
                        new SPProviderHierarchyNode(
                            ProviderInternalName,
                            connection.Name,
                            connection.Name,
                            true));
                }
            }

            hierarchy.AddChild(parentNode);
        }

        private void CreateConnectionsNodes(SPProviderHierarchyTree hierarchy)
        {
            IEnumerable<Connection> enterpriseConnections = null;
            IEnumerable<Connection> socialConnections = null;

            try
            {
                enterpriseConnections = this.auth0Client.GetEnterpriseConnections();
                socialConnections = this.auth0Client.GetSocialConnections();
            }
            catch (Exception ex)
            {
                Utils.LogToULS(ex.ToString(), TraceSeverity.Unexpected, EventSeverity.Error);
            }

            CreateConnectionNodes(hierarchy, EnterpriseHierarchyNode, enterpriseConnections);
            CreateConnectionNodes(hierarchy, SocialHierarchyNode, socialConnections);
        }
    }
}