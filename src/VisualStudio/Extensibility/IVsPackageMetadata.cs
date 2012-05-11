using System.Collections.Generic;
using System.Runtime.InteropServices;
using CoApp.Toolkit.Win32;

namespace CoGet.VisualStudio
{
    [ComImport]
    [Guid("8B3C4B38-632E-436C-8934-4669C6118845")]
    public interface IVsPackageMetadata
    {
        string Id { get; }
        FourPartVersion Version { get; }
        string Title { get; }
        string Description { get; }
        string PublisherName { get; }
        string InstallPath { get; }
    }
}
