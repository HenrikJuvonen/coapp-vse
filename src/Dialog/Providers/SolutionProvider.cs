using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;
using CoApp.Toolkit.Engine.Client;
using System.Threading;
using CoGet.VisualStudio;
using CoGet.Dialog.PackageManagerUI;

namespace CoGet.Dialog.Providers
{
    class SolutionProvider : PackagesProviderBase
    {
        private readonly IUserNotifierServices _userNotifierServices;

        public SolutionProvider(ResourceDictionary resources,
                                ProviderServices providerServices)
            : base(resources, providerServices)
        {
            _userNotifierServices = providerServices.UserNotifierServices;
        }

        public override string Name
        {
            get
            {
                return Resources.Dialog_SolutionProvider;
            }
        }

        public override float SortOrder
        {
            get
            {
                return 0.0f;
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
            return true;
        }

        protected override bool ExecuteCore(PackageItem item)
        {
            return true;
        }

        public override IVsExtension CreateExtension(Package package)
        {
            return new PackageItem(this, package)
            {
                CommandName = Resources.Dialog_ManageButton
            };
        }

        public override string NoItemsMessage
        {
            get
            {
                return Resources.Dialog_SolutionProviderNoItem;
            }
        }

        public override string ProgressWindowTitle
        {
            get
            {
                return Resources.Dialog_ManageProgress;
            }
        }

        protected override string GetProgressMessage(Package package)
        {
            return Resources.Dialog_ManageProgress + package.ToString();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "We want to suppress all errors to show an empty node.")]
        protected override void FillRootNodes()
        {
            PackagesTreeNodeBase node = CreateTreeNodeForPackages("project");
            RootNode.Nodes.Add(node);
        }

    }
}
