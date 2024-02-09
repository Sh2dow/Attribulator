using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using VaultLib.Core.Hashing;

namespace Attribulator.UI
{
    /// <summary>
    /// Interaction logic for RaiderWindow.xaml
    /// </summary>
    public partial class RaiderWindow : Window
    {
        public RaiderWindow()
        {
            InitializeComponent();
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var input = this.InputTextBox.Text;
            var numberStyles = NumberStyles.Number;
            if (input.StartsWith("0x"))
            {
                input = input.Replace("0x", "");
                numberStyles = NumberStyles.HexNumber;
            }

            if (uint.TryParse(input, numberStyles, null, out uint value))
            {
                this.ResultTextBox.Text = HashManager.ResolveVLT(value);
            }
        }
    }
}
