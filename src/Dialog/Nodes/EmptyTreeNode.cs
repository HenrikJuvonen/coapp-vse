using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.ExtensionsExplorer;
using CoApp.Toolkit.Engine.Client;
using System.Threading;

namespace CoGet.Dialog.Providers
{

    /// <summary>
    /// This tree node simply shows no packages.
    /// </summary>
    internal class EmptyTreeNode : PackagesTreeNodeBase
    {

        private readonly string _category;

        public EmptyTreeNode(PackagesProviderBase provider, string category, IVsExtensionsTreeNode parent) :
            base(parent, provider)
        {

            if (category == null)
            {
                throw new ArgumentNullException("category");
            }

            _category = category;
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
            return Enumerable.Empty<Package>();
        }

        public override IEnumerable<Package> GetDetailedPackages(IEnumerable<Package> packages)
        {
            return GetPackages();
        }
    }
}
