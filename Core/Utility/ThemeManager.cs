using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using MahApps.Metro;

namespace CoApp.VSE.Core.Utility
{
    public static class ThemeManager
    {
        private static readonly ResourceDictionary LightResource = new ResourceDictionary { Source = new Uri("pack://application:,,,/CoApp.VSE.Core;component/Styles/BaseLight.xaml") };
        private static readonly ResourceDictionary DarkResource = new ResourceDictionary { Source = new Uri("pack://application:,,,/CoApp.VSE.Core;component/Styles/BaseDark.xaml") };
        private static readonly ResourceDictionary AccentResource = new ResourceDictionary { Source = new Uri("pack://application:,,,/CoApp.VSE.Core;component/Styles/Accent.xaml") };
        private static readonly ResourceDictionary AccentResource2 = new ResourceDictionary { Source = new Uri("pack://application:,,,/CoApp.VSE.Core;component/Styles/Accent2.xaml") };
        
        public static void ChangeTheme(Window window, Theme theme)
        {
            var themeResource = (theme == Theme.Light) ? LightResource : DarkResource;
            var themeResource2 = (theme == Theme.Light) ? DarkResource : LightResource;

            RemoveResourceDictionary(themeResource2, window.Resources);
            ApplyResourceDictionary(themeResource, window.Resources);
            ApplyResourceDictionary(AccentResource, window.Resources);
        }

        public static void ChangeAccent(Window window, bool inSolution)
        {
            var accentResource = inSolution ? AccentResource2 : AccentResource;
            ApplyResourceDictionary(accentResource, window.Resources);
        }

        private static void RemoveResourceDictionary(ResourceDictionary newRd, ResourceDictionary oldRd)
        {
            foreach (DictionaryEntry r in newRd)
            {
                if (oldRd.Contains(r.Key))
                    oldRd.Remove(r.Key);
            }
        }

        private static void ApplyResourceDictionary(ResourceDictionary newRd, ResourceDictionary oldRd)
        {
            foreach (DictionaryEntry r in newRd)
            {
                if (oldRd.Contains(r.Key))
                    oldRd.Remove(r.Key);

                oldRd.Add(r.Key, r.Value);
            }
        }
    }
}
