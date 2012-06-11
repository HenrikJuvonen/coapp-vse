using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.ExtensionsExplorer;
using CoApp.Packaging.Common;
using CoApp.VisualStudio.VsCore;

namespace CoApp.VisualStudio.Dialog.Providers
{
    /// <summary>
    /// This tree node lists all packages from a fixed repository.
    /// </summary>
    internal class SimpleTreeNode : PackagesTreeNodeBase
    {
        private readonly string _name;
        private readonly string _location;
        private readonly string _type;

        public SimpleTreeNode(IVsExtensionsTreeNode parent, PackagesProviderBase provider, string name, string location, string type) :
            base(parent, provider)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            _name = name;
            _location = location;
            _type = type;
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }
        
        public override IEnumerable<IPackage> GetPackages()
        {
            return CoAppWrapper.GetPackages(_type, _location, VsVersionHelper.VsMajorVersion);
        }
    }
}