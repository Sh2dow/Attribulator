using Attribulator.UI.PropertyGrid;
using AttribulatorUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using VaultLib.Core.Data;

namespace Attribulator.UI
{
    public partial class EditFieldsWindow : Window
    {
        private VltCollection collection;

        public EditFieldsWindow(VltCollection collection)
        {
            InitializeComponent();

            this.collection = collection;
            this.Title += collection.Name;

            var data = this.collection.GetData().Select(x => x.Key).ToList();
            foreach (var field in collection.Class.OptionalFields.OrderBy(x => x.Name))
            {
                this.FieldStack.Children.Add(new EditFieldItem(field.Name, data.Contains(field.Name)));
            }
        }

        private void Button_Ok_Click(object sender, RoutedEventArgs e)
        {
            var commands = new List<string>();
            foreach (EditFieldItem field in this.FieldStack.Children)
            {
                if (field.IsChecked != this.collection.HasEntry(field.FieldName))
                {
                    if (field.IsChecked)
                    {
                        commands.Add($"add_field {this.collection.Class.Name} {this.collection.Name} {field.FieldName}");
                    }
                    else
                    {
                        commands.Add($"delete_field {this.collection.Class.Name} {this.collection.Name} {field.FieldName}");
                    }
                }
            }

            if (commands.Count > 0)
            {
                if (!MainWindow.Instance.ExecuteScriptInternal(commands.ToArray()))
                {
                    return;
                }

                MainWindow.Instance.AddScriptLines(commands);
            }

            this.Close();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void FilterTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var fields = this.FieldStack.Children.Cast<EditFieldItem>();
            var text = this.FilterTextBox.Text;
            foreach (var field in fields)
            {
                field.Visibility = Visibility.Collapsed;
                if (field.FieldName.Contains(text, StringComparison.InvariantCultureIgnoreCase))
                {
                    field.Visibility = Visibility.Visible;
                }
            }
        }
    }
}
