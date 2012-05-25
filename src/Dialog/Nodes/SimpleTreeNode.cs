using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.ExtensionsExplorer;
using CoApp.Packaging.Client;

namespace CoApp.VisualStudio.Dialog.Providers
{
    /// <summary>
    /// This tree node lists all packages from a fixed repository.
    /// </summary>
    internal class SimpleTreeNode : PackagesTreeNodeBase
    {
        private readonly string _category;
        private readonly string _type;

        public SimpleTreeNode(PackagesProviderBase provider, string category, IVsExtensionsTreeNode parent, string type) :
            base(parent, provider)
        {
            if (category == null)
            {
                throw new ArgumentNullException("category");
            }

            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            _category = category;
            _type = type;
        }

        public override string Name
        {
            get
            {
                return _category;
            }
        }
        
        public override IEnumerable<Package> GetPackages()
        {
            return CoAppWrapper.GetPackages(_type, VsCore.VsVersionHelper.VsMajorVersion);
        }

        public override IEnumerable<Package> GetDetailedPackages(IEnumerable<Package> packages)
        {
            return CoAppWrapper.GetDetailedPackages(packages);
        }
    }
}