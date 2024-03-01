using AttribulatorUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
                this.FieldStack.Items.Add(new ListBoxItem { Content = new EditFieldItem(field.Name, data.Contains(field.Name)) });
            }
        }

        private void Button_Ok_Click(object sender, RoutedEventArgs e)
        {
            var commands = new List<string>();
            foreach (ListBoxItem item in this.FieldStack.Items)
            {
                var field = item.Content as EditFieldItem;
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
                this.DialogResult = true;
            }

            this.Close();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void FilterTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var fields = this.FieldStack.Items.Cast<ListBoxItem>();
            var text = this.FilterTextBox.Text;
            foreach (var field in fields)
            {
                field.Visibility = Visibility.Collapsed;
                var content = field.Content as EditFieldItem;
                if (content.FieldName.Contains(text, StringComparison.InvariantCultureIgnoreCase))
                {
                    field.Visibility = Visibility.Visible;
                }
            }
        }
    }
}
