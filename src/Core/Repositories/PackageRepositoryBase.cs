using CoApp.Toolkit.Engine.Client;
using System.Threading;

namespace CoGet
{
    using System;
    using System.Linq;

    public abstract class PackageRepositoryBase : IPackageRepository
    {
        protected readonly IProgressProvider _progressProvider;

        public abstract IQueryable<Package> GetPackages();

        public abstract IQueryable<Package> GetDetailedPackages(IQueryable<Package> packages);

        public virtual void InstallPackage(Package package)
        {
            throw new NotSupportedException();
        }

        public virtual void UninstallPackage(Package package)
        {
            throw new NotSupportedException();
        }

        public abstract void SetCancellationTokenSource(CancellationTokenSource cts);
    }
}
