using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace CoApp.VisualStudio.VsCore
{
    [Export(typeof(IOptionsPageActivator))]
    public class OptionsPageActivator : IOptionsPageActivator
    {
        // GUID of the Package Feeds page, defined in FeedOptionsPage.cs
        private const string _packageSourcesGUID = "35DE739E-CE3D-45ED-A222-46755163AA93";

        // GUID of the General page, defined in GeneralOptionsPage.cs
        private const string _generalGUID = "EB451205-CEDF-4EC2-AE48-8A6310601356";

        private Action _closeCallback;
        private readonly IVsUIShell _vsUIShell;

        public OptionsPageActivator() :
            this(ServiceLocator.GetGlobalService<SVsUIShell, IVsUIShell>())
        {
        }

        public OptionsPageActivator(IVsUIShell vsUIShell)
        {
            _vsUIShell = vsUIShell;
        }

        public void NotifyOptionsDialogClosed()
        {
            if (_closeCallback != null)
            {

                // We want to clear the value of _closeCallback before invoking it.
                // Hence copying the value into a local variable.
                Action callback = _closeCallback;
                _closeCallback = null;

                callback();
            }
        }

        public void ActivatePage(string page, Action closeCallback)
        {
            _closeCallback = closeCallback;
            if (page == "General")
            {
                ShowOptionsPage(_generalGUID);
            }
            else if (page == "Package Feeds")
            {
                ShowOptionsPage(_packageSourcesGUID);
            }
            else
            {
                throw new ArgumentOutOfRangeException("page");
            }
        }

        private void ShowOptionsPage(string optionsPageGuid)
        {
            object targetGuid = optionsPageGuid;
            Guid toolsGroupGuid = VSConstants.GUID_VSStandardCommandSet97;
            _vsUIShell.PostExecCommand(ref toolsGroupGuid, (uint)VSConstants.cmdidToolsOptions, (uint)0, ref targetGuid);
        }
    }
}