using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;
using CoApp.Toolkit.Engine.Client;

namespace CoApp.VsExtension.Dialog.Providers
{
    class InstalledProvider : PackagesProviderBase
    {
        public InstalledProvider()
        {
        }

        public override string Name
        {
            get
            {
                return Resources.Dialog_InstalledProvider;
            }
        }

        public override float SortOrder
        {
            get
            {
                return 1.0f;
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

        public override bool CanExecute(PackageItem item)
        {
            return false;
        }

        public override IVsExtension CreateExtension(Package package)
        {
            return new PackageItem(this, package)
            {
                CommandName = Resources.Dialog_UninstallButton
            };
        }

        public override string NoItemsMessage
        {
            get
            {
                return Resources.Dialog_InstalledProviderNoItem;
            }
        }

        public override string ProgressWindowTitle
        {
            get
            {
                return Resources.Dialog_UninstallProgress;
            }
        }

        protected override string GetProgressMessage(Package package)
        {
            return Resources.Dialog_UninstallProgress + package.ToString();
        }

        /*
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "We want to suppress all errors to show an empty node.")]*/
        protected override void FillRootNodes()
        {
            var repository = new InstalledRepository();

            PackagesTreeNodeBase node = CreateTreeNodeForPackages(repository);
            RootNode.Nodes.Add(node);
        }

        protected virtual PackagesTreeNodeBase CreateTreeNodeForPackages(IPackageRepository repository)
        {
            return new SimpleTreeNode(this, "All", RootNode, repository);
        }

    }
}
