using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;
using CoApp.Toolkit.Engine.Client;
using System.Threading;
using CoGet.VisualStudio;
using EnvDTE;

namespace CoGet.Dialog.Providers
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

        public override IEnumerable<Package> GetPackages()
        {
            IEnumerable<Package> installedPackages = Proxy.GetInstalledPackages();
            List<Package> resultPackages = new List<Package>();

            foreach (Project p in _solutionManager.GetProjects())
            {
                PackageReferenceFile packageReferenceFile = new PackageReferenceFile(Path.GetDirectoryName(p.FullName) + "/packages.config");

                IEnumerable<PackageReference> packageReferences = packageReferenceFile.GetPackageReferences();

                foreach (PackageReference package in packageReferences)
                {
                    resultPackages.Add(installedPackages.First(pkg => String.Compare(pkg.Name, package.Name, true) == 0 &&
                                                                      String.Compare(pkg.Version.ToString(), package.Version, true) == 0 &&
                                                                      String.Compare(pkg.Architecture.ToString(), package.Architecture, true) == 0));
                }

            }
            return resultPackages;
        }

        public override IEnumerable<Package> GetDetailedPackages(IEnumerable<Package> packages)
        {
            return Proxy.GetDetailedPackages(packages);
        }
    }
}