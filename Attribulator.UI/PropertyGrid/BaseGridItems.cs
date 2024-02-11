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

    public interface ICommandGenerator : IParent
    {
        void GenerateUpdateCommand();
    }

    public abstract class BaseItem : Control, ICommandGenerator
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

        protected ContextMenu CreateContextMenu()
        {
            var contextMenu = new ContextMenu();

            var menuItem = new MenuItem();
            menuItem.Header = "Generate command";
            menuItem.Click += (sender, e) => this.GenerateUpdateCommand();
            contextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Header = "Generate all commands";
            menuItem.Click += (sender, e) => MainWindow.Instance.EditGrid.GenerateUpdateCommand();
            contextMenu.Items.Add(menuItem);

            return contextMenu;
        }

        protected abstract string GetStringValue();

        public void GenerateUpdateCommand()
        {
            this.GenerateUpdateCommand(this.GetStringValue());
        }
    }

    public abstract class BaseEditItem : BaseItem
    {
        protected string name;
        private string lastValue;
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

            var contextMenu = this.CreateContextMenu();

            var textBlock = this.GetTemplateChild("PART_TextBlock") as TextBlock;
            textBlock.Text = this.name;
            textBlock.Padding = new Thickness(this.padding, 0, 0, 0);
            textBlock.ContextMenu = contextMenu;

            var textBox = this.GetTemplateChild("PART_TextBox") as TextBox;
            var val = this.GetValue();
            textBox.Text = val?.ToString(System.Globalization.CultureInfo.InvariantCulture);
            textBox.ContextMenu = contextMenu;
            this.lastValue = textBox.Text;
            textBox.LostFocus += (s, e) =>
            {
                var textBox = s as TextBox;
                var val = this.GetValue();
                var type = val.GetType();
                try
                {
                    if (textBox.Text != this.lastValue)
                    {
                        var converter = TypeDescriptor.GetConverter(type);
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
                    MessageBox.Show($"{textBox.Text} is not a valid value for {type}", "Property error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    textBox.Text = this.lastValue;
                }
            };
        }

        protected override string GetStringValue()
        {
            return this.GetValue().ToString();
        }

        public abstract IConvertible GetValue();
        public abstract void SetValue(IConvertible value);
    }

    public abstract class BaseBoolItem : BaseItem
    {
        private string name;
        private int padding;
        private CheckBox checkBox;

        public BaseBoolItem(IParent parent, string name, int padding)
        {
            this.parent = parent;
            this.name = name;
            this.padding = padding;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var contextMenu = this.CreateContextMenu();

            var headerText = this.GetTemplateChild("PART_TextBlock") as TextBlock;
            headerText.Text = this.name;
            headerText.ContextMenu = contextMenu;
            headerText.Padding = new Thickness(this.padding, 0, 0, 0);

            var checkBox = this.GetTemplateChild("PART_CheckBox") as CheckBox;
            checkBox.IsChecked = this.GetValue();
            checkBox.ContextMenu = contextMenu;
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

        protected override string GetStringValue()
        {
            return this.GetValue().ToString().ToLower();
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

            var contextMenu = this.CreateContextMenu();

            var headerText = this.GetTemplateChild("PART_TextBlock") as TextBlock;
            headerText.Text = this.name;
            headerText.ContextMenu = contextMenu;
            headerText.Padding = new Thickness(this.padding, 0, 0, 0);

            var comboBox = this.GetTemplateChild("PART_ComboBox") as ComboBox;
            comboBox.ContextMenu = contextMenu;
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

        protected override string GetStringValue()
        {
            return this.GetValue().ToString();
        }

        public abstract Enum GetValue();

        public abstract void SetValue(Enum val);
    }
}
