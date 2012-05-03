using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;
using CoApp.Toolkit.Engine.Client;
using System.Threading;

namespace CoApp.VsExtension.Dialog.Providers
{
    /// <summary>
    /// This tree node lists all packages from a fixed repository.
    /// </summary>
    internal class SimpleTreeNode : PackagesTreeNodeBase
    {
        private IPackageRepository _repository;
        private readonly string _category;

        public IPackageRepository Repository
        {
            get
            {
                return _repository;
            }
        }

        public SimpleTreeNode(PackagesProviderBase provider, string category, IVsExtensionsTreeNode parent, IPackageRepository repository, bool collapseVersion = true) :
            base(parent, provider, collapseVersion)
        {
            if (category == null)
            {
                throw new ArgumentNullException("category");
            }
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }

            _category = category;
            _repository = repository;
        }

        public override string Name
        {
            get
            {
                return _category;
            }
        }
        
        public override IQueryable<Package> GetPackages()
        {
            return Repository.GetPackages().AsQueryable();
        }

        public override IQueryable<Package> GetDetailedPackages(IQueryable<Package> packages)
        {
            return Repository.GetDetailedPackages(packages);
        }

        public override void SetCancellationTokenSourceForRepository(CancellationTokenSource cts)
        {
            Repository.SetCancellationTokenSource(cts);
        }
    }
}