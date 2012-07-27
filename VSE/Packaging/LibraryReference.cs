namespace CoApp.VSE.Packaging
{
    public class LibraryReference
    {
        public LibraryReference(string name, string projectName, string configurationName, bool isChecked)
        {
            Name = name;
            ProjectName = projectName;
            ConfigurationName = configurationName;
            IsChecked = isChecked;
        }
        
        public string Name { get; private set; }
        public string ProjectName { get; private set; }
        public string ConfigurationName { get; private set; }
        public bool IsChecked { get; private set; }
    }
}
