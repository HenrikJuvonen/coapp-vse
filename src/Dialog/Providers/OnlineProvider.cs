using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.ExtensionsExplorer;
using CoApp.Packaging.Common;

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
            InstallPackage(item);
            HideProgressWindow();
            return true;
        }

        protected void InstallPackage(PackageItem item)
        {
            CoAppWrapper.InstallPackage(item.PackageIdentity);
        }
                
        protected override void FillRootNodes()
        {
            RootNode.Nodes.Add(CreateTreeNodeForPackages("all"));
            RootNode.Nodes.Add(CreateTreeNodeForPackages("all,dev"));
        }
    }
}
