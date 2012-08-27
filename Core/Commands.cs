namespace CoApp.VSE.Core
{
    using System.Windows.Input;

    public static class Commands
    {
        public readonly static RoutedCommand AddFilter = new RoutedCommand();
        public readonly static RoutedCommand Reload = new RoutedCommand();
        public readonly static RoutedCommand Browse = new RoutedCommand();
        public readonly static RoutedCommand MarkUpdates = new RoutedCommand();
        public readonly static RoutedCommand MarkUpgrades = new RoutedCommand();
        public readonly static RoutedCommand Apply = new RoutedCommand();
        public readonly static RoutedCommand ShowOptions = new RoutedCommand();
        public readonly static RoutedCommand OpenExternalLink = new RoutedCommand();
        public readonly static RoutedCommand FocusSearch = new RoutedCommand();
        public readonly static RoutedCommand LaunchUpdater = new RoutedCommand();
        public readonly static RoutedCommand MarkStatus = new RoutedCommand();
        public readonly static RoutedCommand ToggleConsole = new RoutedCommand();
        public readonly static RoutedCommand ChangeTheme = new RoutedCommand();
        public readonly static RoutedCommand MoreInformation = new RoutedCommand();

        public readonly static RoutedCommand Cancel = new RoutedCommand();
        public readonly static RoutedCommand Ok = new RoutedCommand();
    }
}
