using System.Linq;
using CoApp.Packaging.Common;
using CoApp.Packaging.Common.Model;

namespace CoApp.VisualStudio
{
    public static class SolutionExtensions
    {
        public static string Path(this IPackage package)
        {
            string architecture =
                    package.Architecture == "x64" ? " (x64)" :
                    package.Architecture == "x86" ? " (x86)" : "";

            return @"c:\ProgramData\Program Files" + architecture + @"\Outercurve Foundation\" + package.CanonicalName.PackageName + @"\";
        }

        public static string Type(this IPackage package)
        {
            if (package.Name.Contains("-common"))
            {
                return "vc";
            }
            else if (package.Flavor.IsWildcardMatch("*vc*"))
            {
                return "vc,lib";
            }
            else if (package.Flavor.IsWildcardMatch("*net*") || (package.Roles.Any(n => n.PackageRole.HasFlag(PackageRole.Assembly) && n.PackageRole.HasFlag(PackageRole.DeveloperLibrary))))
            {
                return "net";
            }

            return "";
        }
    }
}