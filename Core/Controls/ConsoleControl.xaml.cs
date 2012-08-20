using CoApp.Packaging.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using CoApp.Toolkit.Extensions;
using CoApp.VSE.Core.Model;
using CoApp.VSE.Core.Extensions;

namespace CoApp.VSE.Core.Controls
{
    public partial class ConsoleControl
    {
        private Key _modifierKey = Key.None;
        private string _input;
        private bool _isBusy;

        private static readonly string[] Commands = { "help", "filter", "install", "remove", "vs", "reload", "elevate", "apply", "update", "restore" };

        private IEnumerable<Package> _lastPackages; 

        private readonly List<string> _commandHistory = new List<string>();
        private int _commandHistoryCursor;

        private const string PromptString = ">";
        
        public ConsoleControl()
        {
            InitializeComponent();

            ConsoleBox.Document.Blocks.Clear();

            WriteLine(string.Format("Press F2 to toggle console."), Brushes.DarkGoldenrod, FontStyles.Normal, FontWeights.Bold);
            WriteLine(PromptString, FontWeights.Bold);

            DataObject.AddPastingHandler(ConsoleBox, OnPaste);
        }
        
        private void Execute(string input)
        {
            if (input == null)
                return;

            foreach (var command in input.Split(';'))
            {
                var cmd = Regex.Replace(command, @"\s+", " "); // replace multiple whitespaces with single whitespace
                ExecuteCommand(cmd);
            }
        }

        private void ExecuteCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
                return;

            _commandHistory.Add(command);
            _commandHistoryCursor = _commandHistory.Count;
            
            var split = command.Split(' ');

            var first = split[0].ToLowerInvariant();
            
            var parameters = split.Skip(1).ToArray();

            var isCommand = new Func<string, bool>(cmd => cmd.StartsWith(first));
            var isParameter = new Func<string, bool>(param => parameters.Any() && param.StartsWith(parameters[0]));
            
            if (isCommand("help"))
            {
                if (isParameter("filter"))
                {
                    WriteLine("Adds/removes filters");
                    WriteLine("Syntax: filter <...>");
                }
                else if (isParameter("elevate"))
                {
                    WriteLine("Attempts to elevate CoApp");
                    WriteLine("Syntax: elevate");
                }
                else if (isParameter("apply"))
                {
                    WriteLine("Begins applying");
                    WriteLine("Syntax: apply");
                }
                else if (isParameter("update"))
                {
                    WriteLine("Marks all updates and begins applying");
                    WriteLine("Syntax: update");
                }
                else if (isParameter("install"))
                {
                    WriteLine("Marks packages for install");
                    WriteLine("Syntax: install <...>");
                }
                else if (isParameter("vs"))
                {
                    WriteLine("Marks packages for Visual Studio");
                    WriteLine("Syntax: vs <...>");
                }
                else if (isParameter("remove") && !parameters.Any(n => n == "re"))
                {
                    WriteLine("Marks packages for remove");
                    WriteLine("Syntax: remove <...>");
                }
                else if (isParameter("reload") && !parameters.Any(n => n == "re"))
                {
                    WriteLine("Resets query and reloads main view");
                    WriteLine("Syntax: reload");
                }
                else if (isParameter("restore") && !parameters.Any(n => n == "re"))
                {
                    WriteLine("Checks for missing packages and attempts to restore them");
                    WriteLine("Syntax: restore");
                }
                else
                {
                    WriteLine("Commands: help, filter, install, remove, vs, elevate, apply, update, reload, restore");
                }
            }
            else if (isCommand("filter"))
            {
                var filterItemsControl = Module.MainWindow.MainControl.FilterItemsControl;
                var filters = ParseFilters(parameters).ToArray();
                
                // Name
                foreach (var filter in filters[0])
                {
                    filterItemsControl.AddFilter("Name", filter);
                }
                foreach (var filter in filters[1])
                {
                    filterItemsControl.RemoveFilter("Name", filter);
                }

                // Boolean
                var details = new[] { "Is Used In Projects", "Is Dependency", "Is Development Package", "Is Latest Version", "Is Installed", "Is Update", "Is Stable", "Is Wanted" };
                foreach (var detail in details)
                {
                    foreach (var filter in filters[2])
                    {
                        if (detail.Replace(" ", "").ToLowerInvariant().StartsWith(filter))
                        {
                            filterItemsControl.AddFilter("Boolean", detail);
                        }
                    }

                    foreach (var filter in filters[3])
                    {
                        if (detail.Replace(" ", "").ToLowerInvariant().StartsWith(filter))
                        {
                            filterItemsControl.RemoveFilter("Boolean", detail);
                        }
                    }
                }
                

                // Flavor
                details = new[] { "net20", "net35", "net40", "net45", "vc10", "vc11", "vc8", "vc9" };
                foreach (var detail in details)
                {
                    foreach (var filter in filters[4])
                    {
                        if (detail.Replace(" ", "").ToLowerInvariant().StartsWith(filter))
                        {
                            filterItemsControl.AddFilter("Flavor", detail);
                        }
                    }

                    foreach (var filter in filters[5])
                    {
                        if (detail.Replace(" ", "").ToLowerInvariant().StartsWith(filter))
                        {
                            filterItemsControl.RemoveFilter("Flavor", detail);
                        }
                    }
                }

                // Architecture
                details = new[] { "any", "x64", "x86" };
                foreach (var detail in details)
                {
                    foreach (var filter in filters[6])
                    {
                        if (detail.Replace(" ", "").ToLowerInvariant().StartsWith(filter))
                        {
                            filterItemsControl.AddFilter("Architecture", detail);
                        }
                    }

                    foreach (var filter in filters[7])
                    {
                        if (detail.Replace(" ", "").ToLowerInvariant().StartsWith(filter))
                        {
                            filterItemsControl.RemoveFilter("Architecture", detail);
                        }
                    }
                }

                // Role
                details = new[] { "Application", "Assembly", "DeveloperLibrary" };
                foreach (var detail in details)
                {
                    foreach (var filter in filters[8])
                    {
                        if (detail.Replace(" ", "").ToLowerInvariant().StartsWith(filter))
                        {
                            filterItemsControl.AddFilter("Role", detail);
                        }
                    }

                    foreach (var filter in filters[9])
                    {
                        if (detail.Replace(" ", "").ToLowerInvariant().StartsWith(filter))
                        {
                            filterItemsControl.RemoveFilter("Role", detail);
                        }
                    }
                }

                // Feed Url
                details = Module.PackageManager.GetFeedLocations().ToArray();
                foreach (var detail in details)
                {
                    foreach (var filter in filters[10])
                    {
                        if (detail.Replace(" ", "").ToLowerInvariant().StartsWith(filter))
                        {
                            filterItemsControl.AddFilter("Feed URL", detail);
                        }
                    }

                    foreach (var filter in filters[11])
                    {
                        if (detail.Replace(" ", "").ToLowerInvariant().StartsWith(filter))
                        {
                            filterItemsControl.RemoveFilter("Feed URL", detail);
                        }
                    }
                }
            }
            else if (isCommand("elevate"))
            {
                if (Module.PackageManager.Elevate())
                {
                    WriteLine("Elevated");
                }
                else
                {
                    WriteLine("Not elevated");
                }
            }
            else if (isCommand("apply"))
            {
                if (Module.PackageManager.IsAnyMarked)
                {
                    if (Module.PackageManager.VisualStudioPlan.Any())
                        Module.ShowVisualStudioControl();
                    else
                        Module.ShowSummaryControl();
                }
            }
            else if (isCommand("update"))
            {
                foreach (var item in Module.PackageManager.PackagesViewModel.Packages.Where(n => n.Status == PackageItemStatus.InstalledHasUpdate))
                {
                    item.SetStatus(PackageItemStatus.MarkedForUpdate);

                    Module.MainWindow.MainControl.UpdateMarkLists(item);
                }

                if (Module.PackageManager.IsAnyMarked)
                {
                    if (Module.PackageManager.VisualStudioPlan.Any())
                        Module.ShowVisualStudioControl();
                    else
                        Module.ShowSummaryControl();
                }
            }
            else if (isCommand("install"))
            {
                var packages = ParsePackages(parameters);

                foreach (var package in packages)
                {
                    var packageItem = Module.PackageManager.PackagesViewModel.Packages.First(n => n.PackageIdentity == package);

                    if (packageItem.Status == PackageItemStatus.NotInstalled && packageItem.Status != PackageItemStatus.NotInstalledBlocked)
                        packageItem.SetStatus(PackageItemStatus.MarkedForInstallation);

                    Module.MainWindow.MainControl.UpdateMarkLists(packageItem);
                }
            }
            else if (isCommand("vs"))
            {
                var packages = ParsePackages(parameters);

                foreach (var package in packages)
                {
                    var packageItem = Module.PackageManager.PackagesViewModel.Packages.First(n => n.PackageIdentity == package);

                    if (packageItem.Status == PackageItemStatus.Installed && Module.IsSolutionOpen && packageItem.PackageIdentity.GetDeveloperLibraryType() != DeveloperLibraryType.None)
                        packageItem.SetStatus(PackageItemStatus.MarkedForVisualStudio);

                    Module.MainWindow.MainControl.UpdateMarkLists(packageItem);
                }
            }
            else if (isCommand("remove"))
            {
                var packages = ParsePackages(parameters);

                foreach (var package in packages)
                {
                    var packageItem = Module.PackageManager.PackagesViewModel.Packages.First(n => n.PackageIdentity == package);

                    if (packageItem.Status == PackageItemStatus.InstalledLocked)
                        packageItem.SetStatus(PackageItemStatus.MarkedForRemoval);

                    Module.MainWindow.MainControl.UpdateMarkLists(packageItem);
                }
            }
            else if (isCommand("reload") && first != "re")
            {
                Module.ReloadMainControl();
            }
            else if (isCommand("restore") && first != "re")
            {
                Module.RestoreMissingPackages(true);
            }
            else
            {
                WriteLine("Invalid command: " + command, Brushes.Red);
            }
        }

        private IEnumerable<List<string>> ParseFilters(IEnumerable<string> parameters)
        {
            var namesp = new List<string>();
            var namesm = new List<string>();
            var booleansp = new List<string>();
            var booleansm = new List<string>();
            var flavorsp = new List<string>();
            var flavorsm = new List<string>();
            var architecturesp = new List<string>();
            var architecturesm = new List<string>();
            var rolesp = new List<string>();
            var rolesm = new List<string>();
            var feedurlsp = new List<string>();
            var feedurlsm = new List<string>();

            foreach (var parameter in parameters)
            {
                var parameterSplit = 
                    parameter.Contains('+') ? parameter.ToLowerInvariant().Split('+') :
                    parameter.Contains('-') ? parameter.ToLowerInvariant().Split('-') :
                    new string[0];

                var p = parameter.Contains('+');

                if (parameterSplit.Length == 2)
                {
                    var a = parameterSplit[0];
                    var b = parameterSplit[1];

                    if ("name".StartsWith(a))
                    {
                        if (p)
                            namesp.Add(b);
                        else
                            namesm.Add(b);
                    }
                    else if ("boolean".StartsWith(a) && b != "is" && b != "isde")
                    {
                        if (p)
                            booleansp.Add(b);
                        else
                            booleansm.Add(b);   
                    }
                    else if ("flavor".StartsWith(a) && a != "f" && b != "net" && b != "vc")
                    {
                        if (p)
                            flavorsp.Add(b);
                        else
                            flavorsm.Add(b);
                    }
                    else if ("architecture".StartsWith(a) && b != "x")
                    {
                        if (p)
                            architecturesp.Add(b);
                        else
                            architecturesm.Add(b);
                    }
                    else if ("role".StartsWith(a) && b != "a")
                    {
                        if (p)
                            rolesp.Add(b);
                        else
                            rolesm.Add(b);
                    }
                    else if ("feedurl".StartsWith(a) && a != "f" && b != "a")
                    {
                        if (p)
                            feedurlsp.Add(b);
                        else
                            feedurlsm.Add(b);
                    }
                    else
                    {
                        WriteLine("Invalid parameter: " + parameter, Brushes.Red, FontStyles.Normal, FontWeights.Normal);
                    }
                }
            }

            return new[] { namesp, namesm, booleansp, booleansm, flavorsp, flavorsm, architecturesp, architecturesm, rolesp, rolesm, feedurlsp, feedurlsm };
        }

        private IEnumerable<Package> ParsePackages(IEnumerable<string> parameters)
        {
            var searchtexts = new List<string>();
            var names = new List<string>();
            var flavors = new List<string>();
            var minversions = new List<string>();
            var maxversions = new List<string>();
            var architectures = new List<string>();
            bool? stable = null;
            bool? latest = null;
            bool? installed = null;

            foreach (var parameter in parameters)
            {
                var parameterSplit = parameter.ToLowerInvariant().Split('=');

                if (parameterSplit.Length == 2)
                {
                    var a = parameterSplit[0];
                    var b = parameterSplit[1];

                    if ("name".StartsWith(a))
                        names.Add(b);
                    else if ("flavor".StartsWith(a))
                        flavors.Add(b);
                    else if ("minversion".StartsWith(a) && a != "m")
                        minversions.Add(b);
                    else if ("maxversion".StartsWith(a) && a != "m")
                        maxversions.Add(b);
                    else if ("architecture".StartsWith(a))
                        architectures.Add(b);
                    else if ("stable".StartsWith(a))
                        stable = "true".StartsWith(b) ? true : "false".StartsWith(b) ? false : (bool?)null;
                    else if ("latest".StartsWith(a))
                        latest = "true".StartsWith(b) ? true : "false".StartsWith(b) ? false : (bool?)null;
                    else if ("installed".StartsWith(a))
                        installed = "true".StartsWith(b) ? true : "false".StartsWith(b) ? false : (bool?)null;
                    else
                        WriteLine("Invalid parameter: " + parameter, Brushes.Red, FontStyles.Normal, FontWeights.Normal);
                }
                else
                {
                    searchtexts.Add(parameterSplit[0]);
                }
            }

            var packages = Module.PackageManager.PackagesInFeeds.SelectMany(n => n.Value).Where(m => !searchtexts.Any() || searchtexts.Any(n => m.CanonicalName.PackageName.Contains(n)));
            
            _lastPackages = 
                from package in packages
                where !searchtexts.Any() || searchtexts.Any(n => package.Name == n || (package.Name + package.Flavor) == n || (package.Name + package.Flavor + "-" + package.Version) == n || (package.Name + package.Flavor + "-" + package.Version + "-" + package.Architecture) == n)
                where !names.Any() || names.Any(n => package.Name.Contains(n))
                where !flavors.Any() || flavors.Any(n => package.Flavor.ToString().Contains(n))
                where !minversions.Any() || minversions.Any(n => package.Version >= n)
                where !maxversions.Any() || maxversions.Any(n => package.Version <= n)
                where !architectures.Any() || architectures.Any(n => package.Architecture.ToString().Contains(n))
                where stable == true ? package.PackageDetails.Stability == 0 : stable != false || package.PackageDetails.Stability != 0
                where latest == true ? !package.NewerPackages.Any() : latest != false || package.NewerPackages.Any()
                where installed == true ? package.IsInstalled : installed != false || !package.IsInstalled
                orderby package.CanonicalName
                select package;

            return _lastPackages;
        }

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            e.CancelCommand();

            if (e.SourceDataObject.GetDataPresent(DataFormats.Text, true))
            {
                var text = (string) e.SourceDataObject.GetData(DataFormats.Text);
                text = text.Replace("\r\n", ";");
                text = text.Replace("\n", ";");

                _input += text;

                var start = ConsoleBox.Document.Blocks.LastBlock.ContentStart.GetPositionAtOffset(3);
                var sdelta = start.GetOffsetToPosition(ConsoleBox.Selection.Start);

                if (!ConsoleBox.Selection.IsEmpty && sdelta >= 2)
                {
                    ConsoleBox.Selection.Text = text;
                }
                else
                {
                    Write(text, FontWeights.Bold);
                    ConsoleBox.CaretPosition = ConsoleBox.CaretPosition.DocumentEnd;
                }
            }
        }

        private void OnGotFocus(object sender, EventArgs e)
        {
            ConsoleBox.CaretPosition = ConsoleBox.CaretPosition.DocumentEnd;
        }
        
        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ConsoleBox.UndoLimit = 0;

                _input = string.Join("", ((Paragraph)ConsoleBox.Document.Blocks.LastBlock).Inlines.Select(n => ((Run)n).Text));

                if (_input.Length == PromptString.Length)
                    return;

                _input = _input.Substring(PromptString.Length);

                _isBusy = true;
                Execute(_input);
                _isBusy = false;

                _input = string.Empty;
                WriteLine(PromptString, FontWeights.Bold);
                e.Handled = true;

                ConsoleBox.CaretPosition = ConsoleBox.CaretPosition.DocumentEnd;

                ConsoleBox.UndoLimit = -1;
            }

            if (e.Key == Key.LeftCtrl ||
                e.Key == Key.LeftAlt ||
                e.Key == Key.LeftShift ||
                e.Key == Key.RightCtrl ||
                e.Key == Key.RightAlt ||
                e.Key == Key.RightShift)
            {
                _modifierKey = Key.None;
            }
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var start = ConsoleBox.Document.Blocks.LastBlock.ContentStart.GetPositionAtOffset(PromptString.Length + 1);
            var delta = start.GetOffsetToPosition(ConsoleBox.CaretPosition);
            var sdelta = start.GetOffsetToPosition(ConsoleBox.Selection.Start);

            if (Module.IsApplying)
                e.Handled = true;

            if (_isBusy)
            {
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Left || e.Key == Key.Right)
            {
                if (delta <= 2 && e.Key == Key.Left)
                {
                    ConsoleBox.CaretPosition = start;
                    e.Handled = true;
                }
                else if (delta <= 1 && e.Key == Key.Right)
                {
                    ConsoleBox.CaretPosition = start;
                }
                
                return;
            }

            if (e.Key == Key.Escape)
            {
                ConsoleBox.Selection.Select(start, ConsoleBox.CaretPosition.DocumentEnd);
                ConsoleBox.Selection.Text = string.Empty;
                e.Handled = true;
            }

            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                if (e.Key == Key.Up && _commandHistoryCursor > 0)
                    _commandHistoryCursor--;
                else if (e.Key == Key.Down && _commandHistoryCursor < _commandHistory.Count - 1)
                    _commandHistoryCursor++;
                else
                {
                    e.Handled = true;
                    return;
                }

                if (_commandHistory.Any())
                {
                    ConsoleBox.Selection.Select(start, ConsoleBox.CaretPosition.DocumentEnd);
                    ConsoleBox.Selection.Text = string.Empty;
                    Write(_commandHistory[_commandHistoryCursor], FontWeights.Bold);
                    ConsoleBox.CaretPosition = ConsoleBox.CaretPosition.DocumentEnd;
                }

                e.Handled = true;
            }
            
            if (e.Key == Key.LeftCtrl ||
                e.Key == Key.LeftAlt ||
                e.Key == Key.LeftShift ||
                e.Key == Key.RightCtrl ||
                e.Key == Key.RightAlt ||
                e.Key == Key.RightShift)
            {
                _modifierKey = e.Key;
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Delete)
            {
                if (delta >= 2)
                {
                    if (ConsoleBox.Selection.IsEmpty)
                        ConsoleBox.CaretPosition.DeleteTextInRun(1);
                    else if (sdelta >= 2)
                        ConsoleBox.Selection.Text = string.Empty;
                }
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Back)
            {
                if (delta > 2)
                {
                    if (ConsoleBox.Selection.IsEmpty)
                        ConsoleBox.CaretPosition.DeleteTextInRun(-1);
                    else if (sdelta >= 2)
                        ConsoleBox.Selection.Text = string.Empty;
                }
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter)
            {
                e.Handled = true;
            }

            if (_modifierKey == Key.None)
            {
                if (delta < 0)
                    ConsoleBox.CaretPosition = ConsoleBox.CaretPosition.DocumentEnd;
            }
        }
        
        private void Write(string message)
        {
            var lastBlock = (Paragraph) ConsoleBox.Document.Blocks.LastBlock;
            lastBlock.Inlines.Add(new Run(message) { FontWeight = FontWeights.Normal });
            ConsoleBox.ScrollToEnd();
        }

        private void Write(string message, FontWeight weight)
        {
            var lastBlock = (Paragraph)ConsoleBox.Document.Blocks.LastBlock;
            lastBlock.Inlines.Add(new Run(message) { FontWeight = weight });
            ConsoleBox.ScrollToEnd();
        }

        private void Write(string message, Brush brush, FontStyle style, FontWeight weight)
        {
            var lastBlock = (Paragraph)ConsoleBox.Document.Blocks.LastBlock;
            lastBlock.Inlines.Add(new Run(message) { Foreground = brush, FontStyle = style, FontWeight = weight });
            ConsoleBox.ScrollToEnd();
        }

        private void WriteLine(string message)
        {
            ConsoleBox.Document.Blocks.Add(new Paragraph(new Run(message)));
            ConsoleBox.ScrollToEnd();
        }

        private void WriteLine(string message, Brush brush)
        {
            ConsoleBox.Document.Blocks.Add(new Paragraph(new Run(message) { Foreground = brush }));
            ConsoleBox.ScrollToEnd();
        }

        private void WriteLine(string message, FontWeight weight)
        {
            ConsoleBox.Document.Blocks.Add(new Paragraph(new Run(message) { FontWeight = weight }));
            ConsoleBox.ScrollToEnd();
        }
        
        private void WriteLine(string message, Brush brush, FontStyle style, FontWeight weight)
        {
            ConsoleBox.Document.Blocks.Add(new Paragraph(new Run(message) { Foreground = brush, FontStyle = style, FontWeight = weight }));
            ConsoleBox.ScrollToEnd();
        }
    }
}
