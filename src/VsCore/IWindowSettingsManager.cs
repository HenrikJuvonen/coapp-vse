using System.Windows;

namespace CoApp.VisualStudio.VsCore
{
    public interface IWindowSettingsManager
    {
        Size GetWindowSize(string windowToken);
        void SetWindowSize(string windowToken, Size size);
    }
}