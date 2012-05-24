using System;

namespace CoApp.VsExtension.VisualStudio
{
    public interface IVsCommonOperations
    {
        bool OpenFile(string filePath);
        IDisposable SaveSolutionExplorerNodeStates(ISolutionManager solutionManager);
    }
}