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

namespace Attribulator.UI
{
    /// <summary>
    /// Interaction logic for NewNodeName.xaml
    /// </summary>
    public partial class NewNodeNameWindow : Window
    {
        public string Result { get; private set; }

        public NewNodeNameWindow()
        {
            InitializeComponent();
        }

        private void Button_Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(this.NameTextBox.Text.Trim()))
            {
                MessageBox.Show("Enter valid node name", "Invalid node name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.Result = this.NameTextBox.Text;
            this.Close();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
