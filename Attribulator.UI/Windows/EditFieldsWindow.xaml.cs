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
            this.Title += VltUiUtils.GetName(collection);

            var data = this.collection.GetData().Select(x => x.Key).ToList();
            var optionalFields = collection.Class.Fields.Values.Where(x => !x.IsInLayout);
            foreach (var item in optionalFields
                         .Select(f => new { Field = f, Name = VltUiUtils.ResolveName(f.Key) })
                         .OrderBy(x => x.Name, StringComparer.InvariantCultureIgnoreCase))
            {
                var field = item.Field;
                var fieldName = item.Name;
                this.FieldStack.Items.Add(new ListBoxItem
                {
                    Content = new EditFieldItem(fieldName, data.Contains(field.Key))
                });
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
                        commands.Add(
                            $"add_field {VltUiUtils.GetName(this.collection.Class)} {VltUiUtils.GetName(this.collection)} {field.FieldName}");
                    }
                    else
                    {
                        commands.Add(
                            $"delete_field {VltUiUtils.GetName(this.collection.Class)} {VltUiUtils.GetName(this.collection)} {field.FieldName}");
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
