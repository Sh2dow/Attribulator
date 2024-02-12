using AttribulatorUI;

namespace Attribulator.UI.Windows
{
    public class NewNodeNameWindow : BaseInputWindow
    {
        public NewNodeNameWindow(string title, string parent) : base()
        {
            this.HeaderLabel.Content = "New node name:";
            this.Title = $"[{title}] New node name";

            this.OkButton.Click += (s, e) =>
            {
                string command = $"add_node {parent} {this.InputTextBox.Text}";

                if (MainWindow.Instance.ExecuteScriptInternal(command))
                {
                    MainWindow.Instance.AddScriptLine(command);

                    this.DialogResult = true;
                    this.Close();
                }
            };
        }
    }
}
