namespace CoApp.VisualStudio
{
    using System;

    /// <summary>
    /// Used by ProgressProvider
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        public ProgressEventArgs(string operation, string message, int percentComplete)
        {
            Operation = operation;
            Message = message;
            PercentComplete = percentComplete;
        }

        public string Operation { get; private set; }
        public string Message { get; private set; }
        public int PercentComplete { get; private set; }
    }
}
