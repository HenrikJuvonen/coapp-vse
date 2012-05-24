using System;

namespace CoApp.VisualStudio
{
    public interface IProgressProvider
    {
        event EventHandler<ProgressEventArgs> ProgressAvailable;

        void OnProgressAvailable(string operation, int percentage);
    }
}
