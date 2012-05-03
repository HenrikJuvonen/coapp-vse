using System.Collections.Generic;
using System.Linq;
using CoApp.Toolkit.Engine.Client;

namespace CoApp.VsExtension
{
    public interface ISearchableRepository
    {
        IQueryable<Package> Search(string searchTerm);
        IEnumerable<Package> FindPackagesById(string packageId);
    }
}
