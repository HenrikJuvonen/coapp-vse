using System.Windows;
using Microsoft.VisualStudio.ExtensionsExplorer;
using CoApp.Packaging.Common;

namespace CoApp.VisualStudio.Dialog.Providers
{
    internal class UpdatesProvider : PackagesProviderBase
    {
        public UpdatesProvider(ResourceDictionary resources,
                                ProviderServices providerServices)
            : base(resources, providerServices)
        {
        }

        public override string Name
        {
            get
            {
                return Resources.Dialog_UpdateProvider;
            }
        }

        public override float SortOrder
        {
            get
            {
                return 3.0f;
            }
        }

        protected override void FillRootNodes()
        {
            RootNode.Nodes.Add((IVsExtensionsTreeNode)new SimpleTreeNode(RootNode, this, "All", null, "updatable"));
        }

        public override bool CanExecuteCore(PackageItem item)
        {
            return true;
        }

        protected override bool ExecuteCore(PackageItem item)
        {
            CoAppWrapper.SetNewCancellationTokenSource();

            ShowWaitDialog();
            CoAppWrapper.InstallPackage(item.PackageIdentity);

            return true;
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
                return Resources.Dialog_UpdatesProviderNoItem;
            }
        }

        public override string ProgressWindowTitle
        {
            get
            {
                return Resources.Dialog_InstallProgress;
            }
        }
    }
}