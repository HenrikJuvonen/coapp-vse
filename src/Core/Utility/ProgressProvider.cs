using System;
using System.ComponentModel.Composition;

namespace CoApp.VisualStudio
{
    [Export(typeof(IProgressProvider))]
    public class ProgressProvider : IProgressProvider
    {
        public ProgressProvider()
        {
        }

        public event EventHandler<ProgressEventArgs> ProgressAvailable = delegate { };

        public void OnProgressAvailable(string operation, int percentage)
        {
            ProgressAvailable(this, new ProgressEventArgs(operation, percentage));
        }
    }
}
