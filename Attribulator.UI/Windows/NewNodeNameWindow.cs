using AttribulatorUI;
using System;
using System.Windows;

namespace Attribulator.UI.Windows
{
    public class NewNodeNameWindow : BaseInputWindow
    {
        public NewNodeNameWindow(string title, string parent) : base()
        {
            this.HeaderLabel.Content = "New node name:";
            this.Title = $"[{title}] New node name";

            this.OkButton.Click += (s, e) =>
            {
                string command = $"add_node {parent} {this.InputTextBox.Text}";

                try
                {
                    MainWindow.Instance.ExecuteScriptUsafe(new[] { command });
                    MainWindow.Instance.AddScriptLine(command);

                    this.DialogResult = true;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error adding new node", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };
        }
    }
}
