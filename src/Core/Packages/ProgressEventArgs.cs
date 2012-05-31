using System;

namespace CoApp.VisualStudio
{
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
