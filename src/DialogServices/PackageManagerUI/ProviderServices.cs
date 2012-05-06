using CoGet.Dialog.PackageManagerUI;
using CoGet.VisualStudio;

namespace CoGet.Dialog.Providers
{
    public sealed class ProviderServices
    {
        public IProgressWindowOpener ProgressWindow { get; private set; }
        public IProviderSettings ProviderSettings { get; private set; }
        public IUserNotifierServices UserNotifierServices { get; private set; }

        public ProviderServices() :
            this(new ProgressWindowOpener(),
                 new ProviderSettingsManager(),
                 new UserNotifierServices()) 
        {
        }

        public ProviderServices(
            IProgressWindowOpener progressWindow,
            IProviderSettings selectedProviderSettings,
            IUserNotifierServices userNotifierServices)
        {
            ProgressWindow = progressWindow;
            ProviderSettings = selectedProviderSettings;
            UserNotifierServices = userNotifierServices;
        }
    }
}
