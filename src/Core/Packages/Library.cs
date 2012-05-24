namespace CoApp.VisualStudio
{
    public class Library
    {
        public Library(string name, string project, string configuration, bool isSelected)
        {
            Name = name;
            Project = project;
            Configuration = configuration;
            IsSelected = isSelected;
        }

        public string Name { get; private set; }
        public string Project { get; private set; }
        public string Configuration { get; private set; }
        public bool IsSelected { get; private set; }
    }
}
