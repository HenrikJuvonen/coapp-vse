using System;

namespace CoApp.VisualStudio.VsCore
{
    public interface IOptionsPageActivator
    {
        void NotifyOptionsDialogClosed();
        void ActivatePage(OptionsPage page, Action closeCallback);
    }
}