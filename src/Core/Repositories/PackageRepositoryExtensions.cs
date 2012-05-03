using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using CoApp.Toolkit.Engine.Client;
using CoApp.Toolkit.Win32;

namespace CoApp.VsExtension
{
    public static class PackageRepositoryExtensions
    {
        public static bool Exists(this IPackageRepository repository, Package package)
        {
            return repository.Exists(package.CanonicalName);
        }

        public static bool Exists(this IPackageRepository repository, string packageId)
        {
            var existenceLookup = repository;
            if (!String.IsNullOrEmpty(packageId) && (existenceLookup != null))
            {
                return existenceLookup.Exists(packageId);
            }   

            return repository.FindPackage(packageId) != null;
        }

        public static IEnumerable<Package> FindPackagesById(this IPackageRepository repository, string packageId)
        {
            var findPackagesRepository = repository as ISearchableRepository;
            if (findPackagesRepository != null)
            {
                return findPackagesRepository.FindPackagesById(packageId).ToList();
            }
            else
            {
                return FindPackagesByIdCore(repository, packageId);
            }
        }

        internal static IEnumerable<Package> FindPackagesByIdCore(IPackageRepository repository, string packageId)
        {
            return (from p in repository.GetPackages()
                    where p.CanonicalName.ToLower() == packageId
                    orderby p.CanonicalName
                    select p).ToList();
        }

        public static IEnumerable<Package> FindPackages(
            this IPackageRepository repository,
            string packageId)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }

            if (packageId == null)
            {
                throw new ArgumentNullException("packageId");
            }

            IEnumerable<Package> packages = repository.FindPackagesById(packageId);

            return packages;
        }

        public static Package FindPackage(
            this IPackageRepository repository,
            string packageId)
        {
            return repository.FindPackages(packageId).FirstOrDefault();
        }

        public static IQueryable<Package> Search(this IPackageRepository repository, string searchTerm)
        {
            var searchableRepository = repository as ISearchableRepository;
            if (searchableRepository != null)
            {
                return searchableRepository.Search(searchTerm);
            }

            return repository.GetPackages().Find(searchTerm)
                                           .AsQueryable();
        }
    }
}
