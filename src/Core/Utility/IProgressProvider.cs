using System;

namespace CoGet
{
    public interface IProgressProvider
    {
        event EventHandler<ProgressEventArgs> ProgressAvailable;

        void OnProgressAvailable(string operation, int percentage);
    }
}
