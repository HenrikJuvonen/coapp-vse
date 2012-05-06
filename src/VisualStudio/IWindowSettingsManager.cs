using System.Windows;

namespace CoGet.VisualStudio
{
    public interface IWindowSettingsManager
    {
        Size GetWindowSize(string windowToken);
        void SetWindowSize(string windowToken, Size size);
    }
}