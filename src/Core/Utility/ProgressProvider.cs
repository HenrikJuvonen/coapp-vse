namespace CoApp.VisualStudio
{
    using System;

    /// <summary>
    /// Used for updating progress and showing messages.
    /// </summary>
    public class ProgressProvider
    {
        public event EventHandler<ProgressEventArgs> ProgressAvailable = delegate { };

        public void Update(string operation, string message, int percentage = 0)
        {
            ProgressAvailable(this, new ProgressEventArgs(operation, message, percentage));
        }
    }
}
