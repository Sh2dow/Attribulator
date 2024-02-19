using AttribulatorUI;
using System.Windows.Media;

namespace Attribulator.UI.Windows
{
    public class NewNodeNameWindow : BaseInputWindow
    {
        public string ResultName { get;private set; }

        public NewNodeNameWindow(string title, string parent, ImageSource icon) : base(icon)
        {
            this.HeaderLabel.Content = "New node name:";
            this.Title = $"[{title}] New node name";

            this.OkButton.Click += (s, e) =>
            {
                string name = this.InputTextBox.Text;
                string command = $"add_node {parent} {name}";

                if (MainWindow.Instance.ExecuteScriptInternal(command))
                {
                    MainWindow.Instance.AddScriptLine(command);

                    this.DialogResult = true;
                    this.ResultName = name;
                    this.Close();
                }
            };
        }
    }
}
