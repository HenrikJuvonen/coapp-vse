using System.Collections.Generic;
using System.Linq;
using CoApp.Toolkit.Engine.Client;

namespace CoGet
{
    public interface ISearchableRepository
    {
        IQueryable<Package> Search(string searchTerm);
        IEnumerable<Package> FindPackagesById(string packageId);
    }
}
