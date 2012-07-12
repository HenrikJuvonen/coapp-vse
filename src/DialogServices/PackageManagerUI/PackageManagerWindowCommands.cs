using System.Windows.Input;

namespace CoApp.VisualStudio.Dialog.PackageManagerUI
{
    public static class PackageManagerWindowCommands
    {
        public readonly static RoutedCommand PackageOperationCore = new RoutedCommand();
        public readonly static RoutedCommand PackageOperationManage = new RoutedCommand();

        public readonly static RoutedCommand ShowOptionsPage = new RoutedCommand();
        public readonly static RoutedCommand FocusOnSearchBox = new RoutedCommand();
        public readonly static RoutedCommand OpenExternalLink = new RoutedCommand();

        public readonly static RoutedCommand PackageOperationSetWanted = new RoutedCommand();

        public readonly static RoutedCommand LaunchUpdater = new RoutedCommand();
    }
}
