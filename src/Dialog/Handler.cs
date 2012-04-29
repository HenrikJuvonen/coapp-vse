using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoApp.Toolkit.Engine.Client;
using CoApp.Toolkit.Win32;
using System.Threading;
using System.Windows.Threading;

namespace CoApp.Vsp
{
    public class Handler
    {
        private static MainWindow mw = new MainWindow();
        private static Proxy proxy = new Proxy();
        private static IEnumerable<Package> packages, subpackages;
        private static IEnumerable<Feed> feeds;

        private static int initialPullStatus;
        private static bool orderByDescending;
        private static bool orderByName;
        private static bool orderByPublisherName;

        public static void Start()
        {
            App app = new App();
            app.Run(mw);
        }

        public static void PushPackageList(IEnumerable<Package> pkgs)
        {
            mw.packageList.Dispatcher.Invoke((Action)(() =>
            {
                if (orderByName)
                {
                    subpackages = orderByDescending ? pkgs.OrderByDescending(p => p.Name) : pkgs.OrderBy(p => p.Name);
                }
                else if (orderByPublisherName)
                {
                    subpackages = orderByDescending ? pkgs.OrderByDescending(p => p.PublisherName) : pkgs.OrderBy(p => p.PublisherName);
                }
                else
                {
                    subpackages = pkgs;
                }
                mw.packageList.Items.Clear();
                foreach (Package p in subpackages)
                {
                    mw.packageList.Items.Add(p.Name + " " + p.Architecture.ToString() + " " + p.Version.ToString());
                }
                if (mw.packageList.HasItems)
                {
                    mw.packageList.SelectedIndex = 0;
                    Pull("info", mw.packageList.Items[0].ToString().Split(new char[] { ' ' }));
                }
            }));
        }

        public static void PushFeedList(IEnumerable<Feed> fds)
        {
            mw.packageList.Items.Clear();
            foreach (Feed f in fds)
            {
                //window.feedList.Items.Add(f.Location + " " + f.LastScanned + " " + f.IsSession + " " + f.IsSuppressed);
            }
        }

        public static void PushPackageInfo(string[] parameters)
        {
            Package p = packages.Where(pkg => pkg.Name == parameters[0])
                                .Where(pkg => pkg.Architecture.ToString() == parameters[1])
                                .Where(pkg => pkg.Version.ToString() == parameters[2])
                                .First();

            mw.packageInfo.Dispatcher.Invoke((Action)(() =>
            {
                mw.packageInfo.Text = "Name: " + p.Name +
                                        "\nArchitecture: " + p.Architecture.ToString() +
                                        "\nVersion: " + p.Version.ToString() +
                                        "\nLicense: " + p.License;
            }));
        }

        public static void OrderBy(bool descending, bool name, bool publisherName)
        {
            orderByDescending = descending;
            orderByName = name;
            orderByPublisherName = publisherName;
        }

        public static void Reorder()
        {
            if (subpackages != null)
                PushPackageList(subpackages);
        }

        public static void Pull(string command, string[] parameters = null)
        {
            Thread thread = new Thread(() =>
            {
                switch (command)
                {
                    case "list":
                        {
                            if (initialPullStatus == 0)
                            {
                                initialPullStatus = 1;
                                mw.Dispatcher.Invoke((Action)(() => mw.ShowProgress()));
                                packages = proxy.ListPackages(new string[]{"*"});
                                mw.Dispatcher.Invoke((Action)(() => mw.HideProgress()));
                                initialPullStatus = 2;
                            }
                            while (packages == null)
                            {
                                // Wait until packages are retrieved.
                            }

                            subpackages = packages.Where(pkg => pkg.Name.Contains(parameters[0]));
                            if (parameters.Length == 2)
                                subpackages = subpackages.Where(pkg => pkg.Name.Contains(parameters[1]));
                            PushPackageList(subpackages);
                            
                            break;
                        }
                    case "info":
                        {
                            PushPackageInfo(parameters);
                            break;
                        }
                    case "feed":
                        {
                            feeds = proxy.ListFeeds();

                            PushFeedList(feeds);
                            break;
                        }
                }
            });

            if (initialPullStatus != 1)
                thread.Start();
        }
    }
}
