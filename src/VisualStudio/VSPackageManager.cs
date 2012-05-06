using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;

namespace CoApp.VsExtension.VisualStudio
{
    public class VsPackageManager : IVsPackageManager
    {
        public VsPackageManager(IPackageRepository sourceRepository)
        {
        }

    }
}