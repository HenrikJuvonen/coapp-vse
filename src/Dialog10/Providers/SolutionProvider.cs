using System.Windows;
using CoApp.VisualStudio.VsCore;

namespace CoApp.VisualStudio.Dialog.Providers
{
    class SolutionProvider : InstalledProvider
    {  
        public SolutionProvider(ResourceDictionary resources,
                                ProviderServices providerServices,
                                ISolutionManager solutionManager)
            : base(resources, providerServices, solutionManager)
        {
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
        
        public override string NoItemsMessage
        {
            get
            {
                return Resources.Dialog_SolutionProviderNoItem;
            }
        }
                
        protected override void FillRootNodes()
        {
            PackagesTreeNodeBase node = new SolutionTreeNode(this, "All", RootNode, _solutionManager);
            RootNode.Nodes.Add(node);
        }
    }
}
