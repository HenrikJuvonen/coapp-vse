namespace CoApp.VisualStudio
{
    using System;
    using System.ComponentModel.Composition;

    /// <summary>
    /// Used for updating progress and showing messages.
    /// </summary>
    public class ProgressProvider
    {
        public ProgressProvider()
        {
        }

        public event EventHandler<ProgressEventArgs> ProgressAvailable = delegate { };

        public void Update(string operation, string message, int percentage = 0)
        {
            ProgressAvailable(this, new ProgressEventArgs(operation, message, percentage));
        }
    }
}
