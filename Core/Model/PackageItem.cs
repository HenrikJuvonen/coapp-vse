using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CoApp.Packaging.Client;
using CoApp.Packaging.Common;
using CoApp.VSE.Core.Extensions;
using EnvDTE;

namespace CoApp.VSE.Core.Model
{
    public class PackageItem : INotifyPropertyChanged
    {
        private readonly IEnumerable<Package> _dependencies;

        public PackageItemStatus Status { get; private set; }

        public Package PackageIdentity { get; internal set; }

        public PackageItem(Package package)
        {
            PackageIdentity = package;

            _dependencies = Module.PackageManager.IdentifyOwnDependencies(PackageIdentity);

            if (Module.IsSolutionOpen)
                _inSolution = Module.DTE.Solution.Projects.OfType<Project>().Any(m => m.IsSupported() && m.HasPackage(PackageIdentity));

            SetStatus();
        }

        private Brush _background;
        public Brush ItemBackground
        {
            get
            {
                return _background;
            }

            set
            {
                if (_background == value)
                    return;

                _background = value;
                OnPropertyChanged("ItemBackground");
            }
        }
        
        /// <summary>
        /// StatusImage of the package
        /// </summary>
        public BitmapImage StatusImage
        {
            get
            {
                return new BitmapImage(new Uri("pack://application:,,,/CoApp.VSE.Core;component/Resources/" + (int)Status + ".png"));
            }
        }

        private bool _inSolution;
        public bool InSolution
        {
            get
            {
                return _inSolution;
            }

            set
            {
                if (_inSolution == value)
                    return;

                _inSolution = value;
                OnPropertyChanged("InSolution");
                OnPropertyChanged("InSolutionImage");
            }
        }

        public BitmapImage InSolutionImage
        {
            get
            {
                return _inSolution ? new BitmapImage(new Uri("pack://application:,,,/CoApp.VSE.Core;component/Resources/vs.png")) : null;
            }
        }

        public string StatusText
        {
            get { return Enum.GetName(typeof (PackageItemStatus), Status); }
        }

        public void SetStatus(PackageItemStatus status = PackageItemStatus.Unmark)
        {
            if (PackageIdentity == null)
                return;

            if (status == PackageItemStatus.Unmark)
            {
                if (PackageIdentity.IsInstalled)
                {
                    if (PackageIdentity.NewerPackages.Any(n => n.Version == PackageIdentity.NewerPackages.Max(m => m.Version) && !n.IsInstalled))
                        SetStatus(PackageItemStatus.InstalledHasUpdate);
                    else if (PackageIdentity.PackageState.HasFlag(PackageState.DoNotChange))
                        SetStatus(PackageItemStatus.InstalledLocked);
                    else
                        SetStatus(PackageItemStatus.Installed);
                }
                else
                {
                    SetStatus(PackageIdentity.IsBlocked ? PackageItemStatus.NotInstalledBlocked : PackageItemStatus.NotInstalled);
                }

                if (PackageIdentity.IsInstalled && Module.PackageManager.IdentifyDependencies(PackageIdentity).Where(n => n.Name != "coapp").Any(n => !n.IsInstalled))
                    SetStatus(PackageItemStatus.Broken);

                if (IsUpdate)
                    SetStatus(PackageItemStatus.Update);

                return;
            }
            Status = status;
            OnPropertyChanged("Status");
            OnPropertyChanged("StatusImage");
        }

        public bool IsUpdate
        {
            get
            {
                return !PackageIdentity.IsInstalled && PackageIdentity.InstalledPackages.Any() &&
                       PackageIdentity.InstalledPackages.Max(n => n.Version) < PackageIdentity.Version;
            }
        }

        public bool IsHighestInstalled
        {
            get { return Module.PackageManager.IsPackageHighestInstalled(PackageIdentity); }
        }

        /// <summary>
        /// Name of the package
        /// </summary>
        public string Name
        {
            get { return PackageIdentity.Name; }
        }

        /// <summary>
        /// Flavor of the package
        /// </summary>
        public string Flavor
        {
            get { return PackageIdentity.Flavor.Plain; }
        }

        /// <summary>
        /// Version of the package
        /// </summary>
        public string Version
        {
            get { return PackageIdentity.Version; }
        }

        /// <summary>
        /// Architecture of the package
        /// </summary>
        public string Architecture
        {
            get { return PackageIdentity.Architecture; }
        }

        public IEnumerable<string> Dependencies
        {
            get { return _dependencies.Select(n => string.Format("{0}{1}-{2}-{3}", n.Name, n.Flavor, n.Version, n.Architecture)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
