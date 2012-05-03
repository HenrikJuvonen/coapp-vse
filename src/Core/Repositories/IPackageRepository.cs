using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CoApp.Toolkit.Engine.Client;
using System.Threading;

namespace CoApp.VsExtension
{
    public interface IPackageRepository
    {
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This call might be expensive")]
        IQueryable<Package> GetPackages();

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This call might be expensive")]
        IQueryable<Package> GetDetailedPackages(IQueryable<Package> packages);

        void AddPackage(Package package);
        void RemovePackage(Package package);

        void SetCancellationTokenSource(CancellationTokenSource cts);
    }
}
