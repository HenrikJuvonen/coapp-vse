using CoApp.Toolkit.Engine.Client;
using System.Threading;

namespace CoApp.VsExtension
{
    using System;
    using System.Linq;

    public abstract class PackageRepositoryBase : IPackageRepository
    {

        public abstract IQueryable<Package> GetPackages();

        public abstract IQueryable<Package> GetDetailedPackages(IQueryable<Package> packages);

        public virtual void AddPackage(Package package)
        {
            throw new NotSupportedException();
        }

        public virtual void RemovePackage(Package package)
        {
            throw new NotSupportedException();
        }

        public abstract void SetCancellationTokenSource(CancellationTokenSource cts);
    }
}
