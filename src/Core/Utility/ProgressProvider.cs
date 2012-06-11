namespace CoApp.VisualStudio
{
    using System;
    using System.ComponentModel.Composition;

    /// <summary>
    /// Used for updating progress.
    /// </summary>
    public class ProgressProvider
    {
        public ProgressProvider()
        {
        }

        public event EventHandler<ProgressEventArgs> ProgressAvailable = delegate { };

        public void UpdateProgress(string operation, int percentage)
        {
            ProgressAvailable(this, new ProgressEventArgs(operation, percentage));
        }
    }
}
