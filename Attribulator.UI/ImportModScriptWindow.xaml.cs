using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Attribulator.UI
{
    public partial class ImportModScriptWindow : Window
    {
        public List<string> ResultScript { get; private set; } = new List<string>();

        private string[] initialScript;

        private static string[] ignoreCommands = new[] { "version", "game", "ui_text", "ui_image", "#" };

        private string scriptFolder;

        public ImportModScriptWindow(string scriptPath)
        {
            InitializeComponent();

            var lines = File.ReadAllLines(scriptPath);
            this.initialScript = lines;
            this.scriptFolder = Path.GetDirectoryName(scriptPath);
            foreach (string line in lines)
            {
                if (line.StartsWith("ui_text", StringComparison.OrdinalIgnoreCase))
                {
                    this.ModDescription.Text += line.Substring("ui_text".Length) + "\n";
                }

                if (line.StartsWith("ui_image", StringComparison.OrdinalIgnoreCase))
                {
                    var imagePath = Path.Combine(this.scriptFolder, line.Substring("ui_image ".Length));
                    if (File.Exists(imagePath))
                    {
                        this.ModImage.Source = new BitmapImage(new Uri(imagePath));
                    }
                }
            }
        }

        private void PopulateResultScript(string[] lines)
        {
            foreach (string line in lines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                var command = line.Trim();

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
        }

        private void Button_Import_Click(object sender, RoutedEventArgs e)
        {
            this.PopulateResultScript(this.initialScript);
            this.Close();
        }
    }
}
