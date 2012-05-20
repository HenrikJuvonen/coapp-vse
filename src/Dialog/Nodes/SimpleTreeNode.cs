using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;
using CoApp.Toolkit.Engine.Client;
using System.Threading;

namespace CoGet.Dialog.Providers
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
            return CoAppProxy.GetPackagesOfType(_type);
        }

        public override IEnumerable<Package> GetDetailedPackages(IEnumerable<Package> packages)
        {
            return CoAppProxy.GetDetailedPackages(packages);
        }
    }
}