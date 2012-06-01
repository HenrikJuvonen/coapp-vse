using CoApp.VisualStudio.Dialog.PackageManagerUI;
using CoApp.VisualStudio.VsCore;

namespace CoApp.VisualStudio.Dialog.Providers
{
    public sealed class ProviderServices
    {
        public ProgressWindowOpener ProgressWindow { get; private set; }
        public UserNotifierServices UserNotifierServices { get; private set; }

        public ProviderServices() :
            this(new ProgressWindowOpener(),
                 new UserNotifierServices()) 
        {
        }

        public ProviderServices(
            ProgressWindowOpener progressWindow,
            UserNotifierServices userNotifierServices)
        {
            ProgressWindow = progressWindow;
            UserNotifierServices = userNotifierServices;
        }
    }
}
