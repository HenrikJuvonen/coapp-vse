using System.Linq;
using CoApp.Packaging.Client;
using CoApp.Packaging.Common;
using CoApp.Packaging.Common.Model;
using CoApp.Toolkit.Extensions;

namespace CoApp.VisualStudio
{
    public static class PackageExtensions
    {
        private static PackageModel GetPackageModel(this IPackage package)
        {
            var pkm = new PackageManager();
            var model = new PackageModel();

            try
            {
                var atomItem = pkm.GetAtomItem(package.CanonicalName).Result;
                model = atomItem.Model;
            }
            catch
            {
            }

            return model;
        }

        public static string GetPackageDirectory(this IPackage package)
        {
            return string.Format(@"{0}\{1}\{2}\", 
                PackageManagerSettings.CoAppInstalledDirectory[package.Architecture],
                package.GetPackageModel().Vendor.MakeSafeFileName(),
                package.CanonicalName.PackageName);
        }

        public static DeveloperPackageType GetDeveloperPackageType(this IPackage package)
        {
            if (package.Name.Contains("-common"))
            {
                return DeveloperPackageType.VcInclude;
            }
            if (package.Flavor.IsWildcardMatch("*vc*"))
            {
                return DeveloperPackageType.VcLibrary;
            }
            if (package.Flavor.IsWildcardMatch("*net*") || package.Flavor.IsWildcardMatch("*silverlight*")
                || (package.Roles.Any(n => n.PackageRole.HasFlag(PackageRole.Assembly) &&
                                           n.PackageRole.HasFlag(PackageRole.DeveloperLibrary))))
            {
                return DeveloperPackageType.Net;
            }

            return DeveloperPackageType.None;
        }

        public static string GetPackageNameWithoutPublicKeyToken(this IPackage package)
        {
            return string.Format("{0}{1}-{2}-{3}", package.Name, package.Flavor, package.Version, package.Architecture);
        }
    }
}