using System.Linq;
using CoApp.Packaging.Client;
using CoApp.Packaging.Common;
using CoApp.Packaging.Common.Model;

namespace CoApp.VSE.Core.Extensions
{
    using Toolkit.Extensions;

    public static class PackageExtensions
    {
        private static PackageModel GetPackageModel(this IPackage package)
        {
            var pkm = new PackageManager();
            var atomItem = pkm.GetAtomItem(package.CanonicalName).Result;
            return atomItem.Model;
        }

        public static string GetPackageDirectory(this IPackage package)
        {
            string dir = string.Format(@"C:\ProgramData\program files{0}\",
                package.Architecture == "any" ? string.Empty :
                package.Architecture == "x64" ? " (x64)" :
                package.Architecture == "x86" ? " (x86)" :
                string.Empty);
            //PackageManagerSettings.CoAppInstalledDirectory.TryGetValue(package.Architecture, out dir);

            return string.Format(@"{0}\{1}\{2}\", dir,
                package.GetPackageModel().Vendor.MakeSafeFileName(),
                package.CanonicalName.PackageName);
        }
        
        public static DeveloperLibraryType GetDeveloperLibraryType(this IPackage package)
        {
            if (package.Name.Contains("-dev-common"))
            {
                return DeveloperLibraryType.VcInclude;
            }
            if (package.Flavor.IsWildcardMatch("*vc*"))
            {
                if (package.Name.Contains("-dev"))
                    return DeveloperLibraryType.VcLibrary;
                
                return DeveloperLibraryType.None;
            }
            if (package.Flavor.IsWildcardMatch("*net*") || package.Flavor.IsWildcardMatch("*silverlight*")
                || (package.Roles.Any(n => n.PackageRole.HasFlag(PackageRole.Assembly) &&
                                           n.PackageRole.HasFlag(PackageRole.DeveloperLibrary))))
            {
                return DeveloperLibraryType.Net;
            }

            return DeveloperLibraryType.None;
        }
    }
}