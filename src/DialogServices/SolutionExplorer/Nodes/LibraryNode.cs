using System.Collections.Generic;
using System.Windows.Media;
using EnvDTE;

namespace CoApp.VisualStudio.Dialog
{
    public class LibraryNode : FolderNode
    {
        public LibraryNode(Project project, string name) :
            base(project, name, null)
        {
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

        public override ImageSource Icon
        {
            get
            {
                return null;
            }
        }
    }
}