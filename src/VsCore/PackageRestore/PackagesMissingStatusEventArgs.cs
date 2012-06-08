using System;

namespace CoApp.VisualStudio.VsCore
{
    public class PackagesMissingStatusEventArgs : EventArgs
    {
        public bool PackagesMissing { get; private set; }

        public PackagesMissingStatusEventArgs(bool packagesMissing)
        {
            PackagesMissing = packagesMissing;
        }
    }
}