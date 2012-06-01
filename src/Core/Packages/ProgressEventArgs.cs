namespace CoApp.VisualStudio
{
    using System;

    /// <summary>
    /// Used by ProgressProvider
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        public ProgressEventArgs(string operation, int percentComplete)
        {
            Operation = operation;
            PercentComplete = percentComplete;
        }

        public string Operation { get; private set; }
        public int PercentComplete { get; private set; }
    }
}
