using System;

namespace CoApp.VsExtension
{
    public interface IProgressProvider
    {
        event EventHandler<ProgressEventArgs> ProgressAvailable;
    }
}
