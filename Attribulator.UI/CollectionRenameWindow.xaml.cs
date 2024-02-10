using AttribulatorUI;
using System;
using System.Windows;
using VaultLib.Core.Data;

namespace Attribulator.UI
{
    public partial class CollectionRenameWindow : Window
    {
        private VltCollection collection;

        public CollectionRenameWindow(VltCollection collection)
        {
            InitializeComponent();

            this.collection = collection;

            this.NameTextBox.Text = collection.Name;
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Ok_Click(object sender, RoutedEventArgs e)
        {
            if (this.NameTextBox.Text == this.collection.Name)
            {
                this.Close();
            }
            else
            {
                string command = $"rename_node {this.collection.Class.Name} {this.collection.Name} {this.NameTextBox.Text}";

                try
                {
                    MainWindow.Instance.ExecuteScriptUsafe(new[] { command });
                    MainWindow.Instance.AddScriptLine(command);
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error renaming collection", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }
}
