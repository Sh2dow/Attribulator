using AttribulatorUI;
using System.Windows;
using System;
using VaultLib.Core.Data;
using System.Windows.Media;

namespace Attribulator.UI.Windows
{
    public class CollectionRenameWindow : BaseInputWindow
    {
        public CollectionRenameWindow(VltCollection collection, ImageSource icon) : base(icon)
        {
            this.Title = "New collection name";
            this.HeaderLabel.Content = "New name:";
            this.InputTextBox.Text = collection.Name;

            this.OkButton.Click += (s, e) =>
            {
                if (this.InputTextBox.Text == collection.Name)
                {
                    this.DialogResult = false;
                    this.Close();
                }
                else
                {
                    string command = $"rename_node {collection.Class.Name} {collection.Name} {this.InputTextBox.Text}";

                    if (MainWindow.Instance.ExecuteScriptInternal(command))
                    {
                        MainWindow.Instance.AddScriptLine(command);
                        this.DialogResult = true;
                        this.Close();
                    }
                }
            };
        }
    }
}
