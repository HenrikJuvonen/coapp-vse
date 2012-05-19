using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using CoApp.Toolkit.Engine.Client;

namespace CoGet.Dialog
{
    internal class SolutionExplorerViewModel
    {
        private Lazy<FolderNode> _solutionNode;

        public SolutionExplorerViewModel(
            Solution solution,
            Package package,
            Func<Package, Project, string, string, bool?> checkedStateSelector,
            Predicate<Project> enabledStateSelector,
            string type)
        {
            if (solution == null)
            {
                throw new ArgumentNullException("solution");
            }

            _solutionNode = new Lazy<FolderNode>(
                () => SolutionWalker.Walk(solution, package, checkedStateSelector, enabledStateSelector, type));
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
