using CoApp.Packaging.Common;

namespace CoApp.VSE
{
    using System;
    using System.ComponentModel;

    public class ProgressEventArgs : EventArgs, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _status;
        private int _progress;

        public ProgressEventArgs(CanonicalName canonicalName, string status, int progress)
        {
            CanonicalName = canonicalName;
            Status = status;
            Progress = progress;
        }

        public CanonicalName CanonicalName { get; private set; }

        public string Name { get { return CanonicalName.Name; } }
        public string Flavor { get { return CanonicalName.Flavor.Plain; } }
        public string Version { get { return CanonicalName.Version; } }
        public string Architecture { get { return CanonicalName.Architecture; } }

        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                NotifyPropertyChanged("Status");
            }
        }

        public int Progress
        {
            get { return _progress; } 
            set 
            {
                _progress = value;
                NotifyPropertyChanged("Progress");
            }
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class UpdatesAvailableEventArgs : EventArgs
    {
        public UpdatesAvailableEventArgs(int count)
        {
            Count = count;
        }

        public int Count { get; private set; }
    }

    public class LogEventArgs : EventArgs
    {
        public LogEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; private set; }
    }
}
