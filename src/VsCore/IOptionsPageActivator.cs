using System;

namespace CoApp.VisualStudio.VsCore
{
    public interface IOptionsPageActivator
    {
        void NotifyOptionsDialogClosed();
        void ActivatePage(string page, Action closeCallback);
    }
}