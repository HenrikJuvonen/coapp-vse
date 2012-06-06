﻿using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.ExtensionsExplorer;
using CoApp.Packaging.Common;
using CoApp.VisualStudio.VsCore;

namespace CoApp.VisualStudio.Dialog.Providers
{
    /// <summary>
    /// This tree node lists all packages from multiple repositories.
    /// </summary>
    internal class AggregateTreeNode : PackagesTreeNodeBase
    {
        private readonly string _name;
        private readonly string _location;
        private readonly string _type;

        public AggregateTreeNode(IVsExtensionsTreeNode parent, PackagesProviderBase provider, string name, string location, string type) :
            base(parent, provider)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (type == null)
            {
                throw new ArgumentNullException("type");
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
            IEnumerable<IPackage> packages = Enumerable.Empty<IPackage>();

            foreach (IVsExtensionsTreeNode treeNode in Nodes)
            {
                var node = treeNode as PackagesTreeNodeBase;

                if (node != null)
                {
                    packages = packages.Union(node.GetPackages());
                }
            }

            return packages;
        }
    }
}