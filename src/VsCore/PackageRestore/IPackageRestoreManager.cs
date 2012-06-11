﻿using System;
using System.Threading.Tasks;

namespace CoApp.VisualStudio.VsCore
{
    public interface IPackageRestoreManager
    {
        /// <summary>
        /// Begins restoring packages.
        /// </summary>
        /// <param name="fromActivation">if set to <c>false</c>, the method will not show any error message, and will not ask questions.</param>
        void BeginRestore(bool fromActivation);

        /// <summary>
        /// Checks the current solution if there is any package missing.
        /// </summary>
        bool CheckForMissingPackages();

        /// <summary>
        /// Restores the missing packages for the current solution.
        /// </summary>
        void RestoreMissingPackages();
    }
}