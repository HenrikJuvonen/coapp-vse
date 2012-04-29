using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CoApp.Toolkit.Engine.Client;
using System.Threading;

namespace CoApp.Vsp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool filterDev;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void ShowProgress()
        {
            packageInfo.Text = "";
            packageList.Items.Clear();
            progress.Visibility = Visibility.Visible;
        }

        public void HideProgress()
        {
            progress.Visibility = Visibility.Hidden;
        }

        private void packageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // update package info
            if (packageList.HasItems)
            {
                Handler.Pull("info", packageList.SelectedItem.ToString().Split(new char[] { ' ' }));
                customButton1.Content = "Install";
                customButton1.Visibility = Visibility.Visible;
            }
            else
            {
                packageInfo.Text = "";
                customButton1.Visibility = Visibility.Hidden;
            }
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void categoryTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            filterDev = false;

            foreach (TreeViewItem t in categoryTree.Items)
            {
                bool childSelected = false;
                foreach (TreeViewItem u in t.Items)
                {
                    if (t.IsSelected) // Select the first sub-item
                    {
                        t.IsSelected = false;
                        u.IsSelected = true;
                        return;
                    }
                    if (u.IsSelected)
                    {
                        childSelected = true;
                        if (u.Header.Equals("All"))
                        {
                            Handler.Pull("list", new string[] { searchBox.Text });
                        }
                        else if (u.Header.Equals("Libraries"))
                        {
                            filterDev = true;
                            Handler.Pull("list", new string[] { searchBox.Text, filterDev ? "dev" : "" });
                        }
                    }
                }
                t.IsExpanded = t.IsSelected || childSelected ? true : false;
            }
        }

        private void searchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Handler.Pull("list", new string[] { searchBox.Text, filterDev ? "dev" : "" });
        }

        private void sortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (((ComboBoxItem)sortComboBox.SelectedItem).Content.ToString()) {
                case "Name: Ascending": Handler.OrderBy(false, true, false); break;
                case "Name: Descending": Handler.OrderBy(true, true, false); break;
                case "Publisher: Ascending": Handler.OrderBy(false, false, true); break;
                case "Publisher: Descending": Handler.OrderBy(true, false, true); break;
            }
            Handler.Reorder();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
