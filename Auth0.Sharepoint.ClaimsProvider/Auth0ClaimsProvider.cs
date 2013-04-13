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
        public Auth0ClaimsProvider(string displayName)
            : base(displayName)
        {
        }

        public override string Name
        {
            get { throw new NotImplementedException(); }
        }

        public override bool SupportsEntityInformation
        {
            get { throw new NotImplementedException(); }
        }

        public override bool SupportsHierarchy
        {
            get { throw new NotImplementedException(); }
        }

        public override bool SupportsResolve
        {
            get { throw new NotImplementedException(); }
        }

        public override bool SupportsSearch
        {
            get { throw new NotImplementedException(); }
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