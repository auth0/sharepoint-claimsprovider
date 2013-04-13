namespace Auth0.Sharepoint.ClaimsProvider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.SharePoint;
    using Microsoft.SharePoint.Administration;
    using Microsoft.SharePoint.Administration.Claims;
    using Microsoft.SharePoint.Diagnostics;
    using Microsoft.SharePoint.WebControls;

    public class Auth0ClaimsProvider : SPClaimProvider
    {
        public const string DefaultProviderDisplayName = "Federated Users (Auth0)";
        public const string DefaultProviderInternalName = "Auth0ClaimsProvider";

        public Auth0ClaimsProvider(string displayName)
            : base(displayName)
        {
        }

        public override string Name
        {
            get { return this.ProviderInternalName; }
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

        public virtual string ProviderDisplayName
        {
            get { return DefaultProviderDisplayName; }
        }

        public virtual string ProviderInternalName
        {
            get { return DefaultProviderInternalName; }
        }

        protected override void FillClaimTypes(List<string> claimTypes)
        {
            throw new NotImplementedException();
        }

        protected override void FillClaimValueTypes(List<string> claimValueTypes)
        {
            throw new NotImplementedException();
        }

        protected override void FillClaimsForEntity(Uri context, SPClaim entity, List<SPClaim> claims)
        {
            throw new NotImplementedException();
        }

        protected override void FillEntityTypes(List<string> entityTypes)
        {
            throw new NotImplementedException();
        }

        protected override void FillHierarchy(Uri context, string[] entityTypes, string hierarchyNodeID, int numberOfLevels, SPProviderHierarchyTree hierarchy)
        {
            throw new NotImplementedException();
        }

        protected override void FillResolve(Uri context, string[] entityTypes, SPClaim resolveInput, List<PickerEntity> resolved)
        {
            throw new NotImplementedException();
        }

        protected override void FillResolve(Uri context, string[] entityTypes, string resolveInput, List<PickerEntity> resolved)
        {
            throw new NotImplementedException();
        }

        protected override void FillSchema(SPProviderSchema schema)
        {
            throw new NotImplementedException();
        }

        protected override void FillSearch(Uri context, string[] entityTypes, string searchPattern, string hierarchyNodeID, int maxCount, SPProviderHierarchyTree searchTree)
        {
            throw new NotImplementedException();
        }
    }
}