using System.Collections.Generic;
using EnvDTE;

namespace CoApp.VisualStudio.Dialog
{
    public class LibraryNode : ProjectNodeBase
    {
        private readonly Project _project;

        public Project Project
        {
            get
            {
                return _project;
            }
        }

        public LibraryNode(Project project, string name) :
            base(name)
        {
            _project = project;
        }

        public override IEnumerable<Project> GetSelectedProjects()
        {
            if (IsSelected == true && IsEnabled)
            {
                yield return _project;
            }
        }

        public override IEnumerable<Library> GetLibraries()
        {
            yield return new Library(Name, Project.Name, Parent.Name, IsSelected == true);
        }
    }
}