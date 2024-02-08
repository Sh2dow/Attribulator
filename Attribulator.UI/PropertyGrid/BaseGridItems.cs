using AttribulatorUI;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Attribulator.UI.PropertyGrid
{
    public interface IParent
    {
    }

    public interface IParentUpdate : IParent
    {
        void Update();
    }

    public interface ICommandName : IParent
    {
        string GetName();
    }

    public class BaseItem : Control
    {
        protected IParent parent;

        protected void GenerateUpdateCommand(string val)
        {
            if (parent is ICommandName commandName)
            {
                var icm = this as ICommandName;
                string command = $"update_field {commandName.GetName()} {icm.GetName()} {val}";
                command = command.Replace(" [", "["); // TODO find a better way
                MainWindow.Instance.AddScriptLine(command);
            }
        }
    }

    public abstract class BaseEditItem : BaseItem
    {
        private string name;
        private string lastValue;
        private int padding;
        private Type type;

        public BaseEditItem(IParent parent, string name, Type type, int padding)
        {
            this.parent = parent;
            this.name = name;
            this.padding = padding;
            this.type = type;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var headerText = this.GetTemplateChild("PART_TextBlock") as TextBlock;
            headerText.Text = this.name;
            headerText.Padding = new Thickness(this.padding, 0, 0, 0);

            TextBox textBox = this.GetTemplateChild("PART_TextBox") as TextBox;
            var val = this.GetValue();
            textBox.Text = val?.ToString(System.Globalization.CultureInfo.InvariantCulture);
            this.lastValue = textBox.Text;
            textBox.LostFocus += (s, e) =>
            {
                var textBox = s as TextBox;
                try
                {
                    if (textBox.Text != this.lastValue)
                    {
                        var converter = TypeDescriptor.GetConverter(this.type);
                        var result = converter.ConvertFromInvariantString(textBox.Text);
                        this.SetValue(result as IConvertible);
                        this.lastValue = textBox.Text;
                        MainWindow.UnsavedChanges = true;
                        this.GenerateUpdateCommand(textBox.Text);
                        (this.parent as IParentUpdate)?.Update();
                    }
                }
                catch
                {
                    MessageBox.Show($"{textBox.Text} is not a valid value for {this.type}", "Property error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    textBox.Text = this.lastValue;
                }
            };
        }

        public abstract IConvertible GetValue();
        public abstract void SetValue(IConvertible value);
    }

    public abstract class BaseBoolItem : BaseItem
    {
        private string name;
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
            checkBox.Checked += (s, e) =>
            {
                this.SetValue(true);
                (this.parent as IParentUpdate)?.Update();
                this.GenerateUpdateCommand("true");
            };
            checkBox.Unchecked += (s, e) =>
            {
                this.SetValue(false);
                (this.parent as IParentUpdate)?.Update();
                this.GenerateUpdateCommand("false");
            };
        }

        public abstract bool GetValue();

        public abstract void SetValue(bool val);
    }

    public abstract class BaseEnumItem : BaseItem
    {
        private string name;
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
                this.GenerateUpdateCommand(item.Content as string);
                (this.parent as IParentUpdate)?.Update();
            };
        }

        public abstract Enum GetValue();

        public abstract void SetValue(Enum val);
    }
}
