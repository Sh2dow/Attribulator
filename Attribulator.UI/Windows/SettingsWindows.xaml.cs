using FramePFX.Themes;
using System.Windows;

namespace Attribulator.UI.Windows
{
    public partial class SettingsWindows : Window
    {
        public Settings settings { get; private set; }

        public SettingsWindows(Settings settings)
        {
            InitializeComponent();

            this.settings = settings;

            this.DoubleClickCB.IsChecked = settings.Root.OpenCollectionByDoubleClick;

            this.ThemeCB.SelectedIndex = (int)settings.Root.Theme;
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            this.settings.Root.OpenCollectionByDoubleClick = this.DoubleClickCB.IsChecked.Value;
            this.settings.Root.Theme = (ThemeType)this.ThemeCB.SelectedIndex;
            ThemesController.SetTheme((ThemeType)this.ThemeCB.SelectedIndex);
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
