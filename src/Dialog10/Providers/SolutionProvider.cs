using System.Windows;
using CoApp.VisualStudio.VsCore;

namespace CoApp.VisualStudio.Dialog.Providers
{
    class SolutionProvider : InstalledProvider
    {
        private readonly ISolutionManager _solutionManager;

        public SolutionProvider(ResourceDictionary resources,
                                ProviderServices providerServices,
                                ISolutionManager solutionManager)
            : base(resources, providerServices, solutionManager)
        {
            _solutionManager = solutionManager;
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
            RootNode.Nodes.Add(new SolutionTreeNode(this, "All", RootNode, _solutionManager));
        }
    }
}
