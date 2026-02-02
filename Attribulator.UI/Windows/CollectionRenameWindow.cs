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
            this.InputTextBox.Text = VltUiUtils.GetName(collection);

            this.OkButton.Click += (s, e) =>
            {
                if (this.InputTextBox.Text == VltUiUtils.GetName(collection))
                {
                    this.DialogResult = false;
                    this.Close();
                }
                else
                {
                    string command =
                        $"rename_node {VltUiUtils.GetName(collection.Class)} {VltUiUtils.GetName(collection)} {this.InputTextBox.Text}";

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
