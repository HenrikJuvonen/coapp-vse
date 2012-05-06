using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CoApp.Toolkit.Engine.Client;
using System.Threading;

namespace CoGet
{
    public interface IPackageRepository
    {
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This call might be expensive")]
        IQueryable<Package> GetPackages();

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This call might be expensive")]
        IQueryable<Package> GetDetailedPackages(IQueryable<Package> packages);

        void InstallPackage(Package package);
        void UninstallPackage(Package package);

        void SetCancellationTokenSource(CancellationTokenSource cts);
    }
}
