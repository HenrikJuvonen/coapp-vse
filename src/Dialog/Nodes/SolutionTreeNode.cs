using System;
using System.IO;
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
            IEnumerable<IPackage> installedPackages = CoAppWrapper.GetPackages("installed", null, VsVersionHelper.VsMajorVersion);
            ISet<IPackage> resultPackages = new HashSet<IPackage>();

            foreach (Project p in _solutionManager.GetProjects())
            {
                PackageReferenceFile packageReferenceFile = new PackageReferenceFile(Path.GetDirectoryName(p.FullName) + "/coapp.packages.config");

                IEnumerable<PackageReference> packageReferences = packageReferenceFile.GetPackageReferences();

                foreach (PackageReference package in packageReferences)
                {
                    try
                    {
                        resultPackages.Add(installedPackages.First(pkg => pkg.Name == package.Name &&
                                                                          pkg.Version == package.Version &&
                                                                          pkg.Architecture == package.Architecture));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }

            }
            return resultPackages;
        }
    }
}