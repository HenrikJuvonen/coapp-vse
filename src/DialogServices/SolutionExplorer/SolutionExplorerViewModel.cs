using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using CoApp.Packaging.Client;

namespace CoApp.VisualStudio.Dialog
{
    internal class SolutionExplorerViewModel
    {
        private Lazy<FolderNode> _solutionNode;

        public SolutionExplorerViewModel(
            Solution solution,
            PackageReference packageReference)
        {
            if (solution == null)
            {
                throw new ArgumentNullException("solution");
            }

            _solutionNode = new Lazy<FolderNode>(
                () => SolutionWalker.Walk(solution, packageReference));
        }

        public bool HasProjects
        {
            get
            {
                return _solutionNode.Value.HasProjects;
            }
        }

        public IEnumerable<ProjectNodeBase> ProjectNodes
        {
            get
            {
                yield return _solutionNode.Value;
            }
        }

        public IEnumerable<Project> GetSelectedProjects()
        {
            if (_solutionNode.IsValueCreated)
            {
                return _solutionNode.Value.GetSelectedProjects();
            }
            else
            {
                return Enumerable.Empty<Project>();
            }
        }

        public IEnumerable<Library> GetLibraries()
        {
            if (_solutionNode.IsValueCreated)
            {
                return _solutionNode.Value.GetLibraries();
            }
            else
            {
                return Enumerable.Empty<Library>();
            }
        }
    }
}
