using System.Windows;
using System.Windows.Media;

namespace Attribulator.UI.Windows
{
    public partial class BaseInputWindow : Window
    {
        public BaseInputWindow(ImageSource icon)
        {
            InitializeComponent();

            this.Icon = icon;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
