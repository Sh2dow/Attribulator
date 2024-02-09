using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Attribulator.UI
{
    public partial class ImportModScriptWindow : Window
    {
        public List<string> ResultScript { get; private set; } = new List<string>();

        private string[] initialScript;

        private static string[] ignoreCommands = new[] { "version", "game", "ui_text", "ui_image", "file_copy", "folder_create", "ui_control", "#" };

        private string scriptFolder;
        private string scriptPath;

        private Dictionary<string, Dictionary<string, RadioButton>> radioButtonGroups = new Dictionary<string, Dictionary<string, RadioButton>>();
        private Dictionary<string, CheckBox> checkboxes = new Dictionary<string, CheckBox>();

        public ImportModScriptWindow(string scriptPath)
        {
            InitializeComponent();

            this.scriptPath = scriptPath;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var lines = File.ReadAllLines(scriptPath);
            this.initialScript = lines;
            this.scriptFolder = Path.GetDirectoryName(scriptPath);
            foreach (string line in lines)
            {
                try
                {
                    if (line.StartsWith("ui_text", StringComparison.OrdinalIgnoreCase))
                    {
                        this.MainPanel.Children.Add(new TextBlock { Text = line.Substring("ui_text".Length), TextWrapping = TextWrapping.Wrap });
                    }

                    if (line.StartsWith("ui_image", StringComparison.OrdinalIgnoreCase))
                    {
                        var imagePath = Path.Combine(this.scriptFolder, line.Substring("ui_image ".Length));

                        var bitmap = new BitmapImage(new Uri(imagePath));

                        this.ImagePanel.Children.Add(new Image
                        {
                            Width = 200,
                            Height = (float)bitmap.PixelHeight / (float)bitmap.PixelWidth * 200.0,
                            Stretch = System.Windows.Media.Stretch.Uniform,
                            Source = bitmap,
                            Margin = new Thickness(0, 0, 0, 5)
                        });
                    }

                    if (line.StartsWith("ui_control", StringComparison.OrdinalIgnoreCase))
                    {
                        var uiControl = line.Split(' ');
                        var controlType = uiControl[1];
                        int start = line.IndexOf("\"") + 1;
                        var controlText = line.Substring(start, line.Length - 1 - start);

                        if (controlType == "radiobutton")
                        {
                            var controlGroup = uiControl[2];
                            var controlName = uiControl[3];

                            var radioButton = new RadioButton
                            {
                                GroupName = controlGroup,
                                Name = controlName,
                                Content = controlText,
                                Margin = new Thickness(5, 5, 0, 0)
                            };

                            this.MainPanel.Children.Add(radioButton);
                            if (!this.radioButtonGroups.TryGetValue(controlGroup, out var radioButtonGroup))
                            {
                                radioButtonGroup = new Dictionary<string, RadioButton>();
                                this.radioButtonGroups[controlGroup] = radioButtonGroup;
                                radioButton.IsChecked = true;
                            }
                            radioButtonGroup.Add(controlName, radioButton);
                        }

                        if (controlType == "checkbox")
                        {
                            var controlName = uiControl[2];
                            var controlValue = uiControl[3];

                            var checkbox = new CheckBox
                            {
                                Name = controlName,
                                Content = controlText,
                                IsChecked = bool.Parse(controlValue),
                                Margin = new Thickness(5, 5, 0, 0)
                            };

                            this.MainPanel.Children.Add(checkbox);
                            this.checkboxes.Add(controlName, checkbox);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Invalid script line:\n" + line, "Script parse error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    this.Close();
                }
            }

            if (this.ImagePanel.Children.Count == 0)
            {
                Grid.SetColumnSpan(this.MainPanelScroller, 2);
            }
        }

        private void PopulateResultScript(string[] lines)
        {
            bool skip = false;
            bool inOption = false;
            foreach (string line in lines)
            {
                try
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    var command = line.Trim();

                    if (command.StartsWith("ui_option"))
                    {
                        if (!inOption)
                        {
                            skip = true;
                            inOption = true;
                            var uiOption = command.Split(' ');
                            if (uiOption.Length == 3)
                            {
                                var group = this.radioButtonGroups[uiOption[1]];
                                var radioButton = group[uiOption[2]];
                                if (radioButton.IsChecked.HasValue && radioButton.IsChecked.Value)
                                {
                                    skip = false;
                                }
                            }
                            else
                            {
                                var checkbox = this.checkboxes[uiOption[1]];
                                if (checkbox.IsChecked.HasValue && checkbox.IsChecked.Value)
                                {
                                    skip = false;
                                }
                            }
                        }
                        else
                        {
                            inOption = false;
                        }

                        continue;
                    }

                    if (inOption && skip)
                    {
                        continue;
                    }

                    if (command.StartsWith("script", StringComparison.OrdinalIgnoreCase))
                    {
                        var subScript = Path.Combine(this.scriptFolder, command.Substring("script ".Length));
                        this.PopulateResultScript(File.ReadAllLines(subScript));
                    }
                    else
                    {
                        bool ignore = false;
                        foreach (var ignoreCommand in ignoreCommands)
                        {
                            if (command.StartsWith(ignoreCommand))
                            {
                                ignore = true;
                                break;
                            }
                        }

                        if (!ignore)
                        {
                            this.ResultScript.Add(command);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Invalid script line:\n" + line, "Script import error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    this.ResultScript.Clear();
                    break;
                }
            }
        }

        private void Button_Import_Click(object sender, RoutedEventArgs e)
        {
            this.PopulateResultScript(this.initialScript);
            this.Close();
        }
    }
}
