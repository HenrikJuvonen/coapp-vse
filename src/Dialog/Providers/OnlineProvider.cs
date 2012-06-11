using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using Microsoft.VisualStudio.ExtensionsExplorer;
using CoApp.Packaging.Common;
using CoApp.Packaging.Client;

namespace CoApp.VisualStudio.Dialog.Providers
{
    class OnlineProvider : PackagesProviderBase
    {
        public OnlineProvider(ResourceDictionary resources,
                                ProviderServices providerServices)
            : base(resources, providerServices)
        {
        }

        public override string Name
        {
            get
            {
                return Resources.Dialog_OnlineProvider;
            }
        }

        public override float SortOrder
        {
            get
            {
                return 2.0f;
            }
        }

        public override bool RefreshOnNodeSelection
        {
            get
            {
                // only refresh if the current node doesn't have any extensions
                return (SelectedNode == null || SelectedNode.Extensions.Count == 0);
            }
        }

        public override bool CanExecuteCore(PackageItem item)
        {
            return !item.PackageIdentity.IsInstalled;
        }

        public override IVsExtension CreateExtension(IPackage package)
        {
            return new PackageItem(this, package)
            {
                CommandName = Resources.Dialog_InstallButton
            };
        }

        public override string NoItemsMessage
        {
            get
            {
                return Resources.Dialog_OnlineProviderNoItem;
            }
        }

        public override string ProgressWindowTitle
        {
            get
            {
                return Resources.Dialog_InstallProgress;
            }
        }

        protected override string GetProgressMessage(IPackage package)
        {
            return Resources.Dialog_InstallProgress + package.ToString();
        }

        protected override bool ExecuteCore(PackageItem item)
        {
            ShowProgressWindow();
            bool result = CoAppWrapper.InstallPackage(item.PackageIdentity);
            HideProgressWindow();
            return result;
        }
                        
        protected override void FillRootNodes()
        {
            RootNode.Nodes.Add(CreateTreeNodeForPackages("All", null, null));

            IEnumerable<Feed> feeds = CoAppWrapper.GetFeeds();

            IEnumerable<string> hosts = new HashSet<string>(
                feeds.Select(f => 
                {
                    Uri uri = new Uri(f.Location);
                    return uri.Host;
                }));

            foreach (string host in hosts)
            {
                string aggregateName = string.IsNullOrEmpty(host) ? "Local" : host;

                AggregateTreeNode treeNode = new AggregateTreeNode(RootNode, this, aggregateName);

                foreach (Feed f in feeds)
                {
                    Uri uri = new Uri(f.Location);

                    if (uri.Host == host)
                    {
                        string name = uri.GetComponents(UriComponents.Path, UriFormat.UriEscaped);

                        if (aggregateName == "Local")
                        {
                            name = Path.GetFileNameWithoutExtension(name);
                        }

                        treeNode.Nodes.Add(new SimpleTreeNode(treeNode, this, name, f.Location, null));
                    }
                }
                
                RootNode.Nodes.Add(treeNode);
            }
        }
    }
}
