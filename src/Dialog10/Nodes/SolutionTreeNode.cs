using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;
using CoApp.Packaging.Common;
using CoApp.VisualStudio.VsCore;
using EnvDTE;

namespace CoApp.VisualStudio.Dialog.Providers
{
    /// <summary>
    /// This tree node lists all packages from a fixed repository.
    /// </summary>
    internal class SolutionTreeNode : PackagesTreeNodeBase
    {
        private readonly string _category;
        private readonly ISolutionManager _solutionManager;

        public SolutionTreeNode(PackagesProviderBase provider, string category, IVsExtensionsTreeNode parent, ISolutionManager solutionManager) :
            base(parent, provider)
        {
            if (category == null)
            {
                throw new ArgumentNullException("category");
            }

            _category = category;
            _solutionManager = solutionManager;
        }

        public override string Name
        {
            get
            {
                return _category;
            }
        }

        public override IEnumerable<IPackage> GetPackages()
        {
            IEnumerable<IPackage> installedPackages = CoAppWrapper.GetPackages("installed", useFilters: false);
            ISet<IPackage> resultPackages = new HashSet<IPackage>();

            foreach (Project p in _solutionManager.GetProjects())
            {
                PackageReferenceFile packageReferenceFile = new PackageReferenceFile(p.GetDirectory() + "/coapp.packages.config");

                IEnumerable<PackageReference> packageReferences = packageReferenceFile.GetPackageReferences();

                foreach (PackageReference packageReference in packageReferences)
                {
                    var package = installedPackages.FirstOrDefault(pkg => pkg.Name == packageReference.Name &&
                                                                          pkg.Flavor == packageReference.Flavor &&
                                                                          pkg.Version == packageReference.Version &&
                                                                          pkg.Architecture == packageReference.Architecture);

                    if (package != null)
                    {
                        resultPackages.Add(package);
                    }
                }

            }
            return resultPackages;
        }

        protected override IEnumerable<IPackage> ApplyFiltering(IEnumerable<IPackage> query)
        {
            return query;
        }
    }
}