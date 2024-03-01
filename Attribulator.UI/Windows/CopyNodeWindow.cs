using AttribulatorUI;
using System.Windows.Media;

namespace Attribulator.UI.Windows
{
    public class CopyNodeWindow : BaseInputWindow
    {
        public string Result { get; private set; }

        public CopyNodeWindow(ImageSource icon, string original, string parent) : base(icon)
        {
            this.HeaderLabel.Content = "New node name:";
            this.Title = $"Copy node";
            this.InputTextBox.Text = original + "_copy";

            this.OkButton.Click += (s, e) =>
            {
                string name = this.InputTextBox.Text;
                string command = $"copy_node {parent} {name}";

                if (MainWindow.Instance.ExecuteScriptInternal(command))
                {
                    MainWindow.Instance.AddScriptLine(command);
                    this.Result = name;
                    this.DialogResult = true;
                    this.Close();
                }
            };
        }
    }
}
