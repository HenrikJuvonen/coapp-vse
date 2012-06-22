using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using CoApp.Packaging.Client;

namespace CoApp.VisualStudio.Dialog
{
    public class SolutionExplorerViewModel
    {
        private SolutionNode _solutionNode;

        public SolutionExplorerViewModel(
            Solution solution,
            PackageReference packageReference,
            bool replacePackage = false)
        {
            if (solution == null)
            {
                throw new ArgumentNullException("solution");
            }

            _solutionNode = SolutionWalker.Walk(solution, packageReference, replacePackage);
        }

        public bool HasProjects
        {
            get
            {
                return _solutionNode.HasProjects;
            }
        }

        public IEnumerable<ViewModelNodeBase> ViewModelNodes
        {
            get
            {
                yield return _solutionNode;
            }
        }

        public IEnumerable<Project> GetSelectedProjects()
        {
            if (_solutionNode != null)
            {
                return _solutionNode.GetSelectedProjects();
            }
            else
            {
                return Enumerable.Empty<Project>();
            }
        }

        public IEnumerable<Library> GetLibraries()
        {
            if (_solutionNode != null)
            {
                return _solutionNode.GetLibraries();
            }
            else
            {
                return Enumerable.Empty<Library>();
            }
        }
    }
}
