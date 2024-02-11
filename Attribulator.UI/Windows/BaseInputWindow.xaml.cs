using System.Windows;

namespace Attribulator.UI.Windows
{
    public partial class BaseInputWindow : Window
    {
        public BaseInputWindow()
        {
            InitializeComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
