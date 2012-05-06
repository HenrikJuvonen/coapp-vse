﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;
using CoApp.Toolkit.Engine.Client;

namespace CoGet.Dialog.Providers
{
    class OnlineProvider : PackagesProviderBase
    {
        public OnlineProvider(ResourceDictionary resources,
                                ProviderServices providerServices,
                                IProgressProvider progressProvider)
            : base(resources, providerServices, progressProvider)
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

        public override bool CanExecute(PackageItem item)
        {
            PackageItem selected = (PackageItem)SelectedNode.Extensions.Single(i => i == item);

            return !selected.IsInstalled;
        }

        public override IVsExtension CreateExtension(Package package)
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

        protected override string GetProgressMessage(Package package)
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
            ProgressManager.UpdateProgress("", 0);
            Proxy.InstallPackage(item.PackageIdentity);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "We want to suppress all errors to show an empty node.")]
        protected override void FillRootNodes()
        {
            var repository = new OnlineRepository();

            PackagesTreeNodeBase node = CreateTreeNodeForPackages(repository);
            RootNode.Nodes.Add(node);
        }

        protected virtual PackagesTreeNodeBase CreateTreeNodeForPackages(IPackageRepository repository)
        {
            return new SimpleTreeNode(this, "All", RootNode, repository);
        }

    }
}
