using CoApp.VisualStudio.Dialog.PackageManagerUI;
using CoApp.VisualStudio.VsCore;

namespace CoApp.VisualStudio.Dialog.Providers
{
    public sealed class ProviderServices
    {
        public WaitDialog WaitDialog { get; private set; }
        public UserNotifierServices UserNotifierServices { get; private set; }

        public ProviderServices() :
            this(new WaitDialog(),
                 new UserNotifierServices()) 
        {
        }

        public ProviderServices(
            WaitDialog progressWindow,
            UserNotifierServices userNotifierServices)
        {
            WaitDialog = progressWindow;
            UserNotifierServices = userNotifierServices;
        }
    }
}
