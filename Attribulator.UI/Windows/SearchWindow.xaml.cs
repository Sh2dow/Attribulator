using AttribulatorUI;
using System.ComponentModel;
using System.Windows;
using System.Xml.Serialization;

namespace Attribulator.UI.Windows
{
    public partial class SearchWindow : Window
    {
        public SearchWindow(Settings settings)
        {
            InitializeComponent();

            this.NodeTextBox.DataContext = settings.Root.Search;
            this.NodeCheckBox.DataContext = settings.Root.Search;

            this.FieldTextBox.DataContext = settings.Root.Search;
            this.FieldCheckBox.DataContext = settings.Root.Search;

            this.ValueTextBox.DataContext = settings.Root.Search;
            this.ValueCheckBox.DataContext = settings.Root.Search;

            this.FindNextButton.DataContext = MainWindow.Instance.Search;
        }

        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.Find();
        }

        private void FindNextButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.FindNext();
        }
    }
}
