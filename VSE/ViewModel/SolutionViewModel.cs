using System.Collections.Generic;
using CoApp.Packaging.Client;
using CoApp.VSE.Model;
using CoApp.VSE.Packaging;
using CoApp.VSE.Utility;
using EnvDTE;

namespace CoApp.VSE.ViewModel
{
    public class SolutionViewModel
    {
        private readonly SolutionNode _solutionNode;

        public SolutionViewModel(Package package)
        {
            _solutionNode = SolutionWalker.Walk(package);
        }

        public IEnumerable<TreeNodeBase> Nodes
        {
            get
            {
                if (_solutionNode != null)
                    yield return _solutionNode;
            }
        }

        public IEnumerable<Project> CheckedProjects
        {
            get { return _solutionNode.CheckedProjects; }
        }

        public IEnumerable<LibraryReference> LibraryReferences
        {
            get { return _solutionNode.LibraryReferences; }
        }
    }
}
