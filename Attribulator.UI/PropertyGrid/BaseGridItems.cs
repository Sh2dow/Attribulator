using AttribulatorUI;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using VaultLib.Core.Data;

namespace Attribulator.UI.PropertyGrid
{
    public interface IParent
    {
        void Update();
    }

    public abstract class BaseEditItem : Control
    {
        private string name;
        private string lastValue;
        private IParent parent;
        private int padding;

        public BaseEditItem(IParent parent, string name, int padding)
        {
            this.parent = parent;
            this.name = name;
            this.padding = padding;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var headerText = this.GetTemplateChild("PART_TextBlock") as TextBlock;
            headerText.Text = this.name;
            headerText.Padding = new Thickness(this.padding, 0, 0, 0);

            TextBox textBox = this.GetTemplateChild("PART_TextBox") as TextBox;
            var val = this.GetValue();
            textBox.Text = val.ToString(System.Globalization.CultureInfo.InvariantCulture);
            this.lastValue = textBox.Text;
            textBox.LostFocus += (s, e) =>
            {
                var textBox = s as TextBox;
                var type = this.GetValue().GetType();
                try
                {
                    var converter = TypeDescriptor.GetConverter(type);
                    var result = converter.ConvertFromInvariantString(textBox.Text);
                    this.SetValue(result as IConvertible);
                    this.lastValue = textBox.Text;
                    MainWindow.UnsavedChanges = true;
                    this.parent?.Update();
                }
                catch
                {
                    MessageBox.Show($"{textBox.Text} is not a valid value for {type}", "Property error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    textBox.Text = this.lastValue;
                }
            };
        }

        public abstract IConvertible GetValue();
        public abstract void SetValue(IConvertible value);
    }

    public abstract class BaseBoolItem : Control
    {
        private string name;
        private IParent parent;
        private int padding;

        public BaseBoolItem(IParent parent, string name, int padding)
        {
            this.parent = parent;
            this.name = name;
            this.padding = padding;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var headerText = this.GetTemplateChild("PART_TextBlock") as TextBlock;
            headerText.Text = this.name;
            headerText.Padding = new Thickness(this.padding, 0, 0, 0);

            var checkBox = this.GetTemplateChild("PART_CheckBox") as CheckBox;
            checkBox.IsChecked = this.GetValue();
            checkBox.Checked += (s, e) => { this.SetValue(true); this.parent?.Update(); };
            checkBox.Unchecked += (s, e) => { this.SetValue(false); this.parent?.Update(); };
        }

        public abstract bool GetValue();

        public abstract void SetValue(bool val);
    }

    public abstract class BaseEnumItem : Control
    {
        private string name;
        private IParent parent;
        private int padding;

        public BaseEnumItem(IParent parent, string name, int padding)
        {
            this.parent = parent;
            this.name = name;
            this.padding = padding;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var headerText = this.GetTemplateChild("PART_TextBlock") as TextBlock;
            headerText.Text = this.name;
            headerText.Padding = new Thickness(this.padding, 0, 0, 0);

            var comboBox = this.GetTemplateChild("PART_ComboBox") as ComboBox;
            var val = this.GetValue();
            var type = val.GetType();
            var enumNames = type.GetEnumNames();
            foreach (var enumName in enumNames)
            {
                comboBox.Items.Add(new ComboBoxItem { Content = enumName, IsSelected = val.ToString() == enumName });
            }

            comboBox.SelectionChanged += (s, e) =>
            {
                var item = comboBox.SelectedItem as ComboBoxItem;
                this.SetValue(Enum.Parse(type, item.Content as string) as Enum);
                this.parent?.Update();
            };
        }

        public abstract Enum GetValue();

        public abstract void SetValue(Enum val);
    }
}
