using AttribulatorUI;
using System;
using System.Windows;
using System.Windows.Media;
using VaultLib.Core.Data;

namespace Attribulator.UI.Windows
{
    public class ChangeVaultWindow : BaseInputWindow
    {
        public ChangeVaultWindow(VltCollection collection, ImageSource icon) : base(icon)
        {
            this.Title = $"[{collection.Name}] Change vault";

            this.HeaderLabel.Content = "New vault name:";

            this.InputTextBox.Text = collection.Vault.Name;

            this.OkButton.Click += (s, e) =>
            {
                if (this.InputTextBox.Text == collection.Vault.Name)
                {
                    this.DialogResult = false;
                    this.Close();
                }
                else
                {
                    string command = $"change_vault {collection.Class.Name} {collection.Name} {this.InputTextBox.Text}";

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
