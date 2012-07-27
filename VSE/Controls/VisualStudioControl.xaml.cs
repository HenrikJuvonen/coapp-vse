﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CoApp.VSE.Packaging;
using CoApp.VSE.ViewModel;
using CoApp.Packaging.Client;

namespace CoApp.VSE.Controls
{
    public partial class VisualStudioControl
    {
        private readonly Dictionary<Package, SolutionViewModel> _solutions = new Dictionary<Package, SolutionViewModel>(); 

        public VisualStudioControl()
        {
            InitializeComponent();
        }
        
        private void ExecuteCancel(object sender, EventArgs e)
        {
            Module.ShowMainControl();
        }

        private void ExecuteOk(object sender, EventArgs e)
        {
            foreach (var packageNsolution in _solutions)
            {
                var package = packageNsolution.Key;
                var solution = packageNsolution.Value;

                var packageReference = new PackageReference(package, solution.LibraryReferences);

                Module.RemoveExistingSimilarPackages(packageReference, solution.CheckedProjects);
                Module.ManagePackage(packageReference, solution.CheckedProjects, solution.LibraryReferences);
            }

            Module.HideVisualStudioControl();
            Module.PackageManager.ClearVisualStudioMarks();

            if (Module.PackageManager.UpdatePlan.Any() || Module.PackageManager.InstallPlan.Any() || Module.PackageManager.RemovePlan.Any())
                Module.ShowSummaryControl();
            else
                Module.ShowMainControl();
        }

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            SolutionTreeView.DataContext = null;

            var package = (Package) PackagesDataGrid.SelectedItem;

            if (package != null && _solutions.ContainsKey(package))
                SolutionTreeView.DataContext = _solutions[package];
        }

        internal void Initialize()
        {
            SolutionTreeView.DataContext = null;
            PackagesDataGrid.ItemsSource = null;

            lock (this)
            {
                _solutions.Clear();
            }

            var worker = new BackgroundWorker();

            worker.DoWork += (sender, args) => 
                Parallel.ForEach(Module.PackageManager.VisualStudioPlan, package =>
                {
                    lock (this)
                    {
                        if (!_solutions.ContainsKey(package))
                            _solutions.Add(package, new SolutionViewModel(new PackageReference(package)));
                    }
                });

            worker.RunWorkerCompleted += (sender, args) =>
            {
                PackagesDataGrid.ItemsSource = from packageNsolution in _solutions
                                               where packageNsolution.Value.Nodes.Any()
                                               orderby packageNsolution.Key.CanonicalName
                                               select packageNsolution.Key;

                PackagesDataGrid.SelectedIndex = 0;
            };

            worker.RunWorkerAsync();
        }
    }
}