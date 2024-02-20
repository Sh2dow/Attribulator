using System.Windows;

namespace Attribulator.UI.Windows
{
    public partial class SearchWindow : Window
    {
        private Settings settings;

        public SearchWindow(Settings settings)
        {
            InitializeComponent();

            this.settings = settings;

            this.NodeTextBox.DataContext = settings.Root.Search;
            this.NodeCheckBox.DataContext = settings.Root.Search;

            this.FieldTextBox.DataContext = settings.Root.Search;
            this.FieldCheckBox.DataContext = settings.Root.Search;

            this.ValueTextBox.DataContext = settings.Root.Search;
            this.ValueCheckBox.DataContext = settings.Root.Search;
        }

        private void FindButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void FindNextButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
