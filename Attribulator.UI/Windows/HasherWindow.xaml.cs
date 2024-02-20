using System.Windows;
using System.Windows.Controls;
using VaultLib.Core.Hashing;

namespace Attribulator.UI.Windows
{
    public partial class HasherWindow : Window
    {
        public HasherWindow()
        {
            InitializeComponent();
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var input = this.InputTextBox.Text;
            var result = VLT32Hasher.Hash(input);
            this.ResultTextBox.Text = "0x" + result.ToString("x8");    
        }
    }
}
