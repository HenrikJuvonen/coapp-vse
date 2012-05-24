using System;
using CoApp.VisualStudio.VsCore;

namespace CoApp.VisualStudio.Dialog.PackageManagerUI
{
    internal class ProviderSettingsManager : SettingsManagerBase, IProviderSettings
    {
        private const string SettingsRoot = "CoApp.VisualStudio";
        private const string SelectedPropertyName = "SelectedProvider";

        public ProviderSettingsManager() :
            base(ServiceLocator.GetInstance<IServiceProvider>())
        {
        }

        public int SelectedProvider
        {
            get
            {
                return Math.Max(0, ReadInt32(SettingsRoot, SelectedPropertyName));
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                WriteInt32(SettingsRoot, SelectedPropertyName, value);
            }
        }
    }
}