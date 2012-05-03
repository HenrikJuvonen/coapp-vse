using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using CoApp.Toolkit.Engine.Client;
using System.Threading;

namespace CoApp.VsExtension
{
    public class OnlineRepository : PackageRepositoryBase, ISearchableRepository, ICloneableRepository
    {
        private Proxy proxy;

        public OnlineRepository()
        {
            proxy = new Proxy();           
        }
        
        public override IQueryable<Package> GetPackages()
        {
            return proxy.GetAllPackages().AsQueryable();
        }

        public override IQueryable<Package> GetDetailedPackages(IQueryable<Package> packages)
        {
            return proxy.GetDetailedPackages(packages).AsQueryable();
        }

        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "OData expects a lower case value.")]
        public IQueryable<Package> Search(string searchTerm)
        {
            return GetPackages().Find(searchTerm).AsQueryable();
        }

        public IPackageRepository Clone()
        {
            return new OnlineRepository();
        }

        public IEnumerable<Package> FindPackagesById(string packageId)
        {
            return PackageRepositoryExtensions.FindPackagesByIdCore(this, packageId);
        }

        public override void SetCancellationTokenSource(CancellationTokenSource cts)
        {
            proxy.SetCancellationTokenSource(cts);
        }
    }
}