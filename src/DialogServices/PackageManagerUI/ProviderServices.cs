using CoApp.VisualStudio.Dialog.PackageManagerUI;
using CoApp.VisualStudio.VsCore;

namespace CoApp.VisualStudio.Dialog.Providers
{
    public sealed class ProviderServices
    {
        public IProgressWindowOpener ProgressWindow { get; private set; }
        public IUserNotifierServices UserNotifierServices { get; private set; }

        public ProviderServices() :
            this(new ProgressWindowOpener(),
                 new UserNotifierServices()) 
        {
        }

        public ProviderServices(
            IProgressWindowOpener progressWindow,
            IUserNotifierServices userNotifierServices)
        {
            ProgressWindow = progressWindow;
            UserNotifierServices = userNotifierServices;
        }
    }
}
