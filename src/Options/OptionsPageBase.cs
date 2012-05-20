using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using CoGet.VisualStudio;

namespace CoGet.Options
{
    [ComVisible(true)]
    public abstract class OptionsPageBase : DialogPage, IServiceProvider
    {
        protected OptionsPageBase()
        {
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // delay 5 milliseconds to give the Options dialog a chance to close itself
            var timer = new Timer
            {
                Interval = 5
            };
            timer.Tick += OnTimerTick;
            timer.Start();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            var timer = (Timer)sender;
            timer.Stop();
            timer.Dispose();

            var optionsPageActivator = ServiceLocator.GetInstance<IOptionsPageActivator>();
            if (optionsPageActivator != null)
            {
                optionsPageActivator.NotifyOptionsDialogClosed();
            }
        }

        // We override the base implementation of LoadSettingsFromStorage and SaveSettingsToStorage
        // since we already provide settings persistance using the SettingsManager. These two APIs
        // will read/write the tools/options properties to an alternate location, which can cause
        // incorrect behavior if the two copies of the data are out of sync.
        public override void LoadSettingsFromStorage() { }

        public override void SaveSettingsToStorage() { }

        object IServiceProvider.GetService(Type serviceType)
        {
            return this.GetService(serviceType);
        }
    }
}