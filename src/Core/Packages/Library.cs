namespace CoApp.VisualStudio
{
    /// <summary>
    /// Represents a static/dynamic library in a project.
    /// </summary>
    public class Library
    {
        public Library(string name, string projectName, string configurationName, bool isSelected)
        {
            Name = name;
            ProjectName = projectName;
            ConfigurationName = configurationName;
            IsSelected = isSelected;
        }

        public string Name { get; private set; }
        public string ProjectName { get; private set; }
        public string ConfigurationName { get; private set; }
        public bool IsSelected { get; set; }
    }
}
