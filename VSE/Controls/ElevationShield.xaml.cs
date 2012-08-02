﻿using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CoApp.VSE.Controls
{
    public partial class ElevationShield
    {
        public ElevationShield()
        {
            InitializeComponent();
            
            if (Toolkit.Win32.AdminPrivilege.IsRunAsAdmin)
                Visibility = Visibility.Collapsed;
            
            Module.PackageManager.Elevated += () => Application.Current.Dispatcher.BeginInvoke(new Action(() => Visibility = Visibility.Collapsed));
        }

        public void UpdateShield(bool enabled = true)
        {
            Source = enabled ? 
                new BitmapImage(new Uri("pack://application:,,,/CoApp.VSE;component/Resources/UAC-Win7.png")) :
                new BitmapImage(new Uri("pack://application:,,,/CoApp.VSE;component/Resources/UAC-Win7-Disabled.png"));
        }
    }
}
