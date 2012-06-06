using System.Windows;
using Microsoft.VisualStudio.ExtensionsExplorer;
using CoApp.Packaging.Common;

namespace CoApp.VisualStudio.Dialog.Providers
{
    internal class UpdatesProvider : OnlineProvider
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
            RootNode.Nodes.Add(CreateTreeNodeForPackages("All", null, "updatable"));
        }

        public override bool CanExecuteCore(PackageItem item)
        {
            return true;
        }

        public override IVsExtension CreateExtension(IPackage package)
        {
            return new PackageItem(this, package)
            {
                CommandName = Resources.Dialog_UpdateButton
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
                return Resources.Dialog_UpdateProgress;
            }
        }

        protected override string GetProgressMessage(IPackage package)
        {
            return Resources.Dialog_UpdateProgress + package.ToString();
        }
    }
}