using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoGet
{
    public class Library
    {
        private readonly string _name;
        private readonly string _project;
        private readonly string _configuration;
        private readonly bool _isSelected;

        public string Name
        {
            get { return _name; }
        }

        public string Project
        {
            get { return _project; }
        }

        public string Configuration
        {
            get { return _configuration; }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
        }

        public Library(string name, string project, string configuration, bool isSelected)
        {
            _name = name;
            _project = project;
            _configuration = configuration;
            _isSelected = isSelected;
        }
    }
}
