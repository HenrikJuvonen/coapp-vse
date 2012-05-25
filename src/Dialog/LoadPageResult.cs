using System.Collections.Generic;
using CoApp.Packaging.Client;

namespace CoApp.VisualStudio.Dialog.Providers
{
    internal class LoadPageResult
    {
        public LoadPageResult(IEnumerable<Package> packages, int pageNumber, int totalCount)
        {
            Packages = packages;
            PageNumber = pageNumber;
            TotalCount = totalCount;
        }

        public IEnumerable<Package> Packages
        {
            get;
            private set;
        }

        public int TotalCount
        {
            get;
            private set;
        }

        public int PageNumber
        {
            get;
            private set;
        }
    }
}
