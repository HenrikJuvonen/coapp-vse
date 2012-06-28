using System;
using System.Linq;
using CoApp.Packaging.Common;
using CoApp.Packaging.Common.Model;

namespace CoApp.VisualStudio
{
    public static class PackageExtensions
    {
        public static string GetPath(this IPackage package)
        {
            var installedDirectory = PackageManagerSettings.CoAppInstalledDirectory.First(n => n.Key == package.Architecture).Value;

            return string.Format(@"{0}\Outercurve Foundation\{1}\", installedDirectory, package.CanonicalName.PackageName);
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
            else if (package.Flavor.IsWildcardMatch("*net*")
                || (package.Roles.Any(n => n.PackageRole.HasFlag(PackageRole.Assembly) &&
                                           n.PackageRole.HasFlag(PackageRole.DeveloperLibrary))))
            {
                return "net";
            }

            return "";
        }

        public static string GetPackageNameWithoutPublicKeyToken(this IPackage package)
        {
            return package.CanonicalName.PackageName.Substring(0,package.CanonicalName.PackageName.LastIndexOf('-'));
        }
    }
}