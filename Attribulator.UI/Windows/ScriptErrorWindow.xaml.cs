using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Attribulator.UI.Windows
{
    public partial class ScriptErrorWindow : Window
    {
        public ScriptErrorWindow(IEnumerable<string> errors)
        {
            InitializeComponent();

            foreach (string error in errors)
            {
                this.ErrorStack.Items.Add(error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
