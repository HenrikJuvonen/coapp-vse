using System.Collections.Generic;
using CoApp.VSE.Model;
using CoApp.VSE.Packaging;
using CoApp.VSE.VisualStudio;
using EnvDTE;

namespace CoApp.VSE.ViewModel
{
    public class SolutionViewModel
    {
        private readonly SolutionNode _solutionNode;

        public SolutionViewModel(PackageReference packageReference)
        {
            _solutionNode = SolutionWalker.Walk(packageReference);
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
