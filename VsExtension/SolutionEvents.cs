using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace CoApp.VSE.VSPackage
{
    class SolutionEvents : IVsSolutionEvents
    {
        public event EventHandler AfterOpenProject = delegate { };

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            AfterOpenProject(this, EventArgs.Empty);
            return 0;
        }

#region WontImplement

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return 0;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return 0;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return 0;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return 0;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return 0;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return 0;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return 0;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return 0;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            return 0;
        }
        
#endregion
    }
}
