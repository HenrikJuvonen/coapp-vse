using System;

namespace CoGet.VisualStudio
{
    public interface IOptionsPageActivator
    {
        void NotifyOptionsDialogClosed();
        void ActivatePage(OptionsPage page, Action closeCallback);
    }
}