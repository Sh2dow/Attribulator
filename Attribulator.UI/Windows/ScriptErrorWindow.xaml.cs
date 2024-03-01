using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Attribulator.UI.Windows
{
    public partial class ScriptErrorWindow : Window
    {
        public ScriptErrorWindow(IEnumerable<ScriptErrorItem> errors)
        {
            InitializeComponent();

            foreach (var error in errors)
            {
                this.ErrorStack.Items.Add(new ListBoxItem { Content = error });
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
