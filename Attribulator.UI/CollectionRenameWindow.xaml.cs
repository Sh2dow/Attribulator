using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
            string newName = this.NameTextBox.Text;
            string error = null;
            if (string.IsNullOrEmpty(newName))
            {
                error = "Enter a valid name";
            }

            if (string.IsNullOrEmpty(error))
            {
                this.collection.SetName(newName);
                this.Close();
            }
            else
            {
                MessageBox.Show(error, "Error renaming collection");
            }
        }
    }
}
