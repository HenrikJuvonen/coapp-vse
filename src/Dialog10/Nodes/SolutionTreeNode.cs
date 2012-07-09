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
            var installedPackages = CoAppWrapper.GetPackages("installed", useFilters: false);
            var resultPackages = new HashSet<IPackage>();

            foreach (var project in _solutionManager.GetProjects())
            {
                var packageReferenceFile = new PackageReferenceFile(project.GetDirectory() + "/coapp.packages.config");

                var packageReferences = packageReferenceFile.GetPackageReferences();

                foreach (var packageReference in packageReferences)
                {
                    var package = installedPackages.FirstOrDefault(pkg => packageReference.Equals(pkg));

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