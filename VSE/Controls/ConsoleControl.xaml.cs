using CoApp.Packaging.Client;

namespace CoApp.VSE.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using Toolkit.Extensions;

    public partial class ConsoleControl
    {
        private Key _modifierKey = Key.None;
        private string _input;
        private bool _isBusy;

        private static readonly string[] Commands = { "help", "list", "install", "remove", "add" };

        private IEnumerable<Package> _lastPackages; 

        private readonly List<string> _commandHistory = new List<string>();
        private int _commandHistoryCursor;

        private const string PromptString = "> ";

        private string _category;

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

            if (command == ".")
            {
                _category = null;
                return;
            }

            var split = command.Split(' ');

            var first = split[0].ToLowerInvariant();
            var firstSplit = first.Split('.');

            if (firstSplit.Length == 1)
            {
                firstSplit = new[] { first, string.Empty, string.Empty };
            }
            if (firstSplit.Length == 2)
            {
                firstSplit = new[] { firstSplit[0], firstSplit[1], string.Empty };
            }

            var parameters = split.Skip(1).ToArray();

            var category = _category;
            if (!Commands.Any(n => n.StartsWith(firstSplit[0])))
            {
                if ((_category == "coapp" && "feed".StartsWith(firstSplit[0])) || ("coapp".StartsWith(firstSplit[0]) && (firstSplit[1] != string.Empty && "feed".StartsWith(firstSplit[1]))))
                    category = "coapp.feed";
                else if ("coapp".StartsWith(firstSplit[0]))
                    category = "coapp";
                else if ("vs".StartsWith(firstSplit[0]) && Module.IsDTELoaded)
                    category = "vs";
                else
                    goto Error;

                if (!Commands.Any(n => (firstSplit[1] != string.Empty && n.StartsWith(firstSplit[1])) || (firstSplit[2] != string.Empty && n.StartsWith(firstSplit[2])))
                    && (first == firstSplit[0] || (first == firstSplit[0] + "." + firstSplit[1] && firstSplit[1] != string.Empty) ||
                    (firstSplit[1] != string.Empty && firstSplit[2] != string.Empty)))
                {
                    _category = category;
                    return;
                }
            }

            var isCategory = new Func<string, bool>(cat => cat == category);
            var isCommand = new Func<string, bool>(cmd => cmd.StartsWith(first) || (firstSplit[1] != string.Empty && cmd.StartsWith(firstSplit[1])) || (firstSplit[2] != string.Empty && cmd.StartsWith(firstSplit[2])));
            var isParameter = new Func<string, bool>(param => parameters.Any() && param.StartsWith(parameters[0]));

            if (isCommand("help"))
            {
                if (isCategory(null) || ".help".StartsWith(first))
                {
                    WriteLine("coapp   Commands for managing packages and feeds.");

                    if (Module.IsDTELoaded)
                        Write("\nvs      Commands for listing, adding and removing packages in projects.");

                    return;
                }
                
                if (isCategory("coapp"))
                {
                    if (isParameter("list"))
                        WriteLine("Lists all packages specified in the parameters.\n\nSyntax: list (<text>) (name=<name>) (flavor=<flavor>) (minversion=<version>) (maxversion=<version>) (architecture=<architecture>) (latest=<boolean>) (stable=<boolean>) (installed=<boolean>)\n\nExamples:\n\n  list\n  list coapp\n  list apr zlib");
                    else if (isParameter("install"))
                        WriteLine("Installs all packages specified in the parameters.\n\nSyntax: install <packages>\n\nExamples:\n\n  install coapp\n  install apr zlib");
                    else if (isParameter("remove"))
                        WriteLine("Removes all packages specified in the parameters.\n\nSyntax: remove <packages>\n\nExamples:\n\n  remove coapp\n  remove apr zlib");
                    else if (isParameter("feed"))
                        WriteLine("list    Lists all feeds.\nadd     Adds feeds specified in the parameters.\nremove  Removes feeds specified in the parameters.");
                    else
                        WriteLine("list    Lists all packages specified in the parameters.\ninstall Installs all packages specified in the parameters.\nremove  Removes all packages specified in the parameters.\nfeed    Commands for managing feeds.");

                    return;
                }
                
                if (isCategory("coapp.feed"))
                {
                    if (isParameter("list"))
                        WriteLine("Lists all feeds.");
                    else if (isParameter("add"))
                        WriteLine("Adds feeds specified in the parameters.\n\nSyntax: add <feeds>\n\nExamples:\n\n  add http://coapp.org/current\n  add http://coapp.org/current http://coapp.org/archive");
                    else if (isParameter("remove"))
                        WriteLine("Removes feeds specified in the parameters.\n\nSyntax: remove <feeds>\n\nExamples:\n\n  remove http://coapp.org/current\n  remove http://coapp.org/current http://coapp.org/archive");
                    else
                        WriteLine("list    Lists all feeds.\nadd     Adds feeds specified in the parameters.\nremove  Removes feeds specified in the parameters.");

                    return;
                }
                
                if (isCategory("vs"))
                {
                    if (isParameter("list"))
                        WriteLine("Lists all packages added in projects.");
                    else if (isParameter("add"))
                        WriteLine("Adds packages in projects.\n\nSyntax: add {<file>} in {<project(:configuration)>} from {<package>}\n\nExamples:\n\n  add * in * from zlib-dev[vc10]-x.y.z.w-x64\n  add zlib zlib1 glib in CppProject from zlib-dev[vc10]-x.y.z.w-x64 glib-dev[vc10]-x.y.z.w-x64\n  add * in CppProject from zlib-dev-common-x.y.z.w-x64\n  add * in * from zlib-dev*x64\n  add *_d in CppProject:Debug from zlib*x64");
                    else if (isParameter("remove"))
                        WriteLine("Removes packages in projects.\n\nSyntax: remove {<file>} in {<project(:configuration)>} from {<package>}\n\nExamples:\n\n  remove * in * from *");
                    else
                        WriteLine("list    Lists all packages added in projects.\nadd     Adds packages in projects.\nremove  Removes packages in projects.");

                    return;
                }
            }

            // don't allow ".command" except ".help" (which was handled above)
            if (firstSplit[0] == string.Empty && firstSplit[1] != string.Empty)
                goto Error;

            if (isCommand("list"))
            {
                if (isCategory("coapp"))
                {
                    var packages = ParsePackages(parameters);

                    var table = (from package in packages
                                 select new
                                 {
                                     package.Name,
                                     Flavor = package.Flavor.Plain,
                                     package.Version,
                                     Arch = package.Architecture,
                                     State = package.PackageState,
                                     Act = package.IsActive,
                                     Blk = package.IsBlocked
                                 }).ToTable().ToArray();

                    for (var i = 0; i < table.Count(); i++)
                    {
                        Write(table[i]);

                        if (i != table.Count() - 1)
                            Write("\n");
                    }

                    return;
                }

                if (isCategory("coapp.feed"))
                {
                    var feeds = Module.PackageManager.GetFeedLocations();

                    WriteLine(string.Empty);

                    var table = (from feed in feeds
                                 select new
                                 {
                                     Location = feed
                                 }).ToTable().ToArray();

                    for (var i = 0; i < table.Count(); i++)
                    {
                        Write(table[i]);

                        if (i != table.Count() - 1)
                            Write("\n");
                    }

                    return;
                }
                if (isCategory("vs"))
                {
                    // List
                    return;
                }
            }

            if (isCommand("install"))
            {
                if (isCategory("coapp"))
                {
                    var packages = parameters.Any() ? ParsePackages(parameters) : _lastPackages;

                    Module.PackageManager.AddMarks(packages, Mark.DirectInstall);
                    Module.ShowProgressControl();
                    return;
                }
            }

            if (isCommand("remove"))
            {
                if (isCategory("coapp"))
                {
                    var packages = parameters.Any() ? ParsePackages(parameters) : _lastPackages;

                    Module.PackageManager.AddMarks(packages, Mark.DirectRemove);
                    Module.ShowProgressControl();
                    return;
                }
                if (isCategory("coapp.feed"))
                {
                    foreach (var parameter in parameters)
                        Module.PackageManager.RemoveFeed(parameter);
                    return;
                }
                if (isCategory("vs"))
                {
                    // Remove
                    return;
                }
            }

            if (isCommand("add"))
            {
                if (isCategory("coapp.feed"))
                {
                    foreach (var parameter in parameters)
                        Module.PackageManager.AddFeed(parameter);
                    return;
                }
                if (isCategory("vs"))
                {
                    // Add
                    return;
                }
            }
            
            Error: WriteLine("Invalid command: " + command, Brushes.Red);
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

            WriteLine(string.Empty);

            foreach (var parameter in parameters)
            {
                var parameterSplit = parameter.ToLowerInvariant().Split('=');

                if (parameterSplit.Length == 2)
                {
                    if (parameterSplit[0] == "m") // for min/max-parameters
                        Write("Invalid parameter: " + parameter + "\n", Brushes.Red, FontStyles.Normal, FontWeights.Normal);

                    else if ("name".StartsWith(parameterSplit[0]))          names.Add(parameterSplit[1]);
                    else if ("flavor".StartsWith(parameterSplit[0]))        flavors.Add(parameterSplit[1]);
                    else if ("minversion".StartsWith(parameterSplit[0]))    minversions.Add(parameterSplit[1]);
                    else if ("maxversion".StartsWith(parameterSplit[0]))    maxversions.Add(parameterSplit[1]);
                    else if ("architecture".StartsWith(parameterSplit[0]))  architectures.Add(parameterSplit[1]);
                    else if ("stable".StartsWith(parameterSplit[0]))        stable = "true".StartsWith(parameterSplit[1]) ? true : "false".StartsWith(parameterSplit[1]) ? false : (bool?)null;
                    else if ("latest".StartsWith(parameterSplit[0]))        latest = "true".StartsWith(parameterSplit[1]) ? true : "false".StartsWith(parameterSplit[1]) ? false : (bool?)null;
                    else if ("installed".StartsWith(parameterSplit[0]))     installed = "true".StartsWith(parameterSplit[1]) ? true : "false".StartsWith(parameterSplit[1]) ? false : (bool?)null;
                }
                else
                {
                    searchtexts.Add(parameterSplit[0]);
                }
            }

            var packages = Module.PackageManager.PackagesInFeeds.SelectMany(n => n.Value).Where(m => !searchtexts.Any() || searchtexts.Any(n => m.CanonicalName.PackageName.Contains(n)));

            _lastPackages = 
                from package in packages
                where !searchtexts.Any() || searchtexts.Any(n => package.CanonicalName.PackageName.Contains(n))
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

        public ConsoleControl()
        {
            InitializeComponent();

            ConsoleBox.Document.Blocks.Clear();

            WriteLine(string.Format("{0}\nPress F2 to toggle console.", GetType().Assembly.FullName), Brushes.DarkGoldenrod, FontStyles.Normal, FontWeights.Bold);
            WriteLine(PromptString, Brushes.Black, FontStyles.Normal, FontWeights.Bold);

            DataObject.AddPastingHandler(ConsoleBox, OnPaste);
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
                    Write(text, Brushes.Black, FontStyles.Normal, FontWeights.Bold);
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
                
                _input = _input.Substring(2);

                _isBusy = true;
                Execute(_input);
                _isBusy = false;

                _input = string.Empty;
                WriteLine(PromptString, Brushes.Black, FontStyles.Normal, FontWeights.Bold);
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
            var start = ConsoleBox.Document.Blocks.LastBlock.ContentStart.GetPositionAtOffset(3);
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
                if ((delta < 3 && e.Key == Key.Left) || (delta < 2 && e.Key == Key.Right))
                {
                    ConsoleBox.CaretPosition = start;
                    e.Handled = true;
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
                    Write(_commandHistory[_commandHistoryCursor], Brushes.Black, FontStyles.Normal, FontWeights.Bold);
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
                if (delta < 2)
                    ConsoleBox.CaretPosition = ConsoleBox.CaretPosition.DocumentEnd;
            }
        }
        
        private void Write(string message)
        {
            var lastBlock = (Paragraph) ConsoleBox.Document.Blocks.LastBlock;
            lastBlock.Inlines.Add(new Run(message) { FontWeight = FontWeights.Normal });
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
            ConsoleBox.Document.Blocks.Add(new Paragraph(new Run(message) { Foreground = brush, FontStyle = FontStyles.Normal, FontWeight = FontWeights.Normal }));
            ConsoleBox.ScrollToEnd();
        }
        
        private void WriteLine(string message, Brush brush, FontStyle style, FontWeight weight)
        {
            ConsoleBox.Document.Blocks.Add(new Paragraph(new Run(message) { Foreground = brush, FontStyle = style, FontWeight = weight }));
            ConsoleBox.ScrollToEnd();
        }
    }
}
