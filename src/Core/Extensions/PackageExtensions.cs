using System;
using System.Linq;
using System.Collections.Generic;
using CoApp.Packaging.Client;
using CoApp.Packaging.Common;
using CoApp.Packaging.Common.Model;

namespace CoApp.VisualStudio
{
    public static class PackageExtensions
    {
        private static PackageModel GetPackageModel(this IPackage package)
        {
            PackageManager pkm = new PackageManager();
            PackageModel model = new PackageModel();

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

        public static string GetPath(this IPackage package)
        {
            var installedDirectory = PackageManagerSettings.CoAppInstalledDirectory.First(n => n.Key == package.Architecture).Value;

            return string.Format(@"{0}\{1}\{2}\", installedDirectory, package.GetPackageModel().Vendor, package.CanonicalName.PackageName);
        }

        public static string GetDevType(this IPackage package)
        {
            if (package.Name.Contains("-common"))
            {
                return "vc";
            }
            else if (package.Flavor.IsWildcardMatch("*vc*"))
            {
                return "vc,lib";
            }
            else if (package.Flavor.IsWildcardMatch("*net*") || package.Flavor.IsWildcardMatch("*silverlight*")
                || (package.Roles.Any(n => n.PackageRole.HasFlag(PackageRole.Assembly) &&
                                           n.PackageRole.HasFlag(PackageRole.DeveloperLibrary))))
            {
                return "net";
            }

            return "";
        }

        public static string GetPackageNameWithoutPublicKeyToken(this IPackage package)
        {
            return string.Format("{0}{1}-{2}-{3}", package.Name, package.Flavor, package.Version, package.Architecture);
        }
    }
}