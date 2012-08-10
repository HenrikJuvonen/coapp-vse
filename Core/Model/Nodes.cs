using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;
using CoApp.VSE.Core.Packaging;
using CoApp.VSE.Core.Utility;
using EnvDTE;

namespace CoApp.VSE.Core.Model
{
    public class TreeNodeBase : INotifyPropertyChanged
    {
        public TreeNodeBase(string name, ICollection<TreeNodeBase> children)
        {
            Name = name;
            Children = children;

            if (Children == null)
                return;

            foreach (var child in Children)
            {
                child.Parent = this;
            }
            OnChildCheckedChanged();
        }

        public string Name { get; private set; }
        public ImageSource Icon { get; protected set; }
        public ICollection<TreeNodeBase> Children { get; private set; }
        public TreeNodeBase Parent { get; private set; }
        
        private bool? _isChecked = false;
        public bool? IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (_isChecked == value)
                    return;

                _isChecked = value;
                OnCheckedChanged();
                OnPropertyChanged("IsChecked");
            }
        }

        private bool _suppressPropagatingIsCheckedProperty;

        // invoked whenever one of its descendent nodes has its IsChecked property changed directly by user.
        internal void OnChildCheckedChanged()
        {
            // Here we detect the IsChecked states of all the direct children.
            // If all children are checked, mark this node as checked.
            // If all children are unchecked, mark this node as unchecked.
            // Otherwise, mark this node as Indeterminate state.

            bool isAllChecked = true, isAllUnchecked = true;
            foreach (var child in Children)
            {
                if (child.IsChecked != true)
                {
                    isAllChecked = false;
                }
                if (child.IsChecked != false)
                {
                    isAllUnchecked = false;
                }
            }

            // don't propagate the change back to children.
            // otherwise, we'll fall into an infinite loop.
            _suppressPropagatingIsCheckedProperty = true;
            if (isAllChecked)
            {
                IsChecked = true;
            }
            else if (isAllUnchecked)
            {
                IsChecked = false;
            }
            else
            {
                IsChecked = null;
            }
            _suppressPropagatingIsCheckedProperty = false;
        }

        private void OnCheckedChanged()
        {
            if (Parent != null)
            {
                Parent.OnChildCheckedChanged();
            }

            if (_suppressPropagatingIsCheckedProperty)
            {
                return;
            }

            if (Children != null)
            {
                var isChecked = IsChecked;
                // propagate the IsChecked value down to all children, recursively
                foreach (var child in Children)
                {
                    child.IsChecked = isChecked;
                }
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SolutionNode : TreeNodeBase
    {
        public SolutionNode(string name, ICollection<TreeNodeBase> children)
            : base(name, children)
        {
            Icon = ProjectUtilities.GetSolutionImage();
        }

        public IEnumerable<Project> CheckedProjects
        {
            get { return Children.OfType<ProjectNode>().Where(n => n.IsChecked != false).Select(n => n.Project); }
        }

        public IEnumerable<LibraryReference> LibraryReferences
        {
            get
            {
                foreach (var project in Children.OfType<ProjectNode>())
                {
                    foreach (var assembly in project.Children.OfType<AssemblyNode>())
                    {
                        yield return new LibraryReference(assembly.Name, project.Name, null, assembly.IsChecked == true);
                    }
                    foreach (var configuration in project.Children.OfType<ConfigurationNode>())
                    {
                        foreach (var library in configuration.Children.OfType<LibraryNode>())
                        {
                            yield return new LibraryReference(library.Name, project.Name, configuration.Name, library.IsChecked == true);
                        }
                    }
                }
            }
        }
    }

    public class ProjectNode : TreeNodeBase
    {
        public Project Project { get; private set; }

        public ProjectNode(Project project, ICollection<TreeNodeBase> children)
            : base(project.Name, children)
        {
            Project = project;
            Icon = ProjectUtilities.GetImage(Project);
        }
    }

    public class ConfigurationNode : TreeNodeBase
    {
        public ConfigurationNode(string configurationName, ICollection<TreeNodeBase> children)
            : base(configurationName, children)
        {
        }
    }

    public class LibraryNode : TreeNodeBase
    {
        public LibraryNode(string libraryName)
            : base(libraryName, null)
        {
        }
    }

    public class AssemblyNode : TreeNodeBase
    {
        public AssemblyNode(string assemblyName)
            : base(assemblyName, null)
        {
        }
    }
}
