using CoApp.Toolkit.Engine.Client;
using CoApp.Toolkit.Win32;

namespace CoGet
{
    public interface IPackagePathResolver
    {
        /// <summary>
        /// Gets the physical installation path of a package
        /// </summary>
        string GetInstallPath(Package package);

        /// <summary>
        /// Gets the package directory name
        /// </summary>
        string GetPackageDirectory(Package package);

        string GetPackageDirectory(string canonicalName);

        /// <summary>
        /// Gets the package file name
        /// </summary>
        string GetPackageFileName(Package package);

        string GetPackageFileName(string canonicalName);
    }
}
