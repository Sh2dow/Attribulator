using System;
using System.Windows;
using System.Windows.Media;

namespace Attribulator.UI.Windows
{
    public partial class ColorPickerWindow : Window
    {
        private Color color;

        public uint Result { get; private set; }

        public ColorPickerWindow(uint rgba)
        {
            InitializeComponent();
            var bytes = BitConverter.GetBytes(rgba);
            this.color.R = bytes[3];
            this.color.G = bytes[2];
            this.color.B = bytes[1];
            this.color.A = bytes[0];

            this.SetTextBox();
            this.SetButtonBackground();
        }

        private void SetButtonBackground()
        {
            this.ColorPickerBtn.Background = new SolidColorBrush(new Color { R = this.color.R, G = this.color.G, B = this.color.B, A = 255 });
        }

        private void SetTextBox()
        {
            this.RedTB.Text = this.color.R.ToString();
            this.GreenTB.Text = this.color.G.ToString();
            this.BlueTB.Text = this.color.B.ToString();
            this.AlphaTB.Text = this.color.A.ToString();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.Result = (uint)((this.color.R << 24) | (this.color.G << 16) | (this.color.B << 8) | this.color.A);

            this.DialogResult = true;
            this.Close();
        }

        private void ColorPickerBtn_Click(object sender, RoutedEventArgs e)
        {
            var colordialog = new System.Windows.Forms.ColorDialog();
            var result = colordialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                this.color.R = colordialog.Color.R;
                this.color.G = colordialog.Color.G;
                this.color.B = colordialog.Color.B;

                this.SetTextBox();
            }
        }

        private void RedTB_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (byte.TryParse(this.RedTB.Text, out byte red))
            {
                this.color.R = red;
                this.SetButtonBackground();
            }
            else
            {
                this.RedTB.Text = this.color.R.ToString();
            }
        }

        private void GreenTB_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (byte.TryParse(this.GreenTB.Text, out byte green))
            {
                this.color.G = green;
                this.SetButtonBackground();
            }
            else
            {
                this.GreenTB.Text = this.color.G.ToString();
            }
        }

        private void BlueTB_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (byte.TryParse(this.BlueTB.Text, out byte blue))
            {
                this.color.B = blue;
                this.SetButtonBackground();
            }
            else
            {
                this.BlueTB.Text = this.color.B.ToString();
            }
        }

        private void AlphaTB_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (byte.TryParse(this.AlphaTB.Text, out byte alpha))
            {
                this.color.A = alpha;
            }
            else
            {
                this.AlphaTB.Text = this.color.A.ToString();
            }
        }
    }
}
