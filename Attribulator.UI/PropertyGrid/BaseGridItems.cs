using Attribulator.UI.Windows;
using AttribulatorUI;
using System;
using System.ComponentModel;
using System.Globalization;
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
        private TextBox textBox;

        public BaseEditItem(IParent parent, string name, int padding)
        {
            this.parent = parent;
            this.name = name;
            this.padding = padding;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            textBox = this.GetTemplateChild("PART_TextBox") as TextBox;
            var contextMenu = this.CreateContextMenu();
            var val = this.GetValue();
            var type = val.GetType();
            if (type == typeof(uint))
            {
                var menuItem = new MenuItem();
                menuItem.Header = "Edit as color (RGBA)";
                menuItem.Click += (sender, e) =>
                {
                    var val = this.GetValue();
                    var colorDialog = new ColorPickerWindow(Convert.ToUInt32(val));
                    if (colorDialog.ShowDialog().Value)
                    {
                        textBox.Text = colorDialog.Result.ToString();
                        this.TextBoxUpdated(null, null);
                    }
                };

                contextMenu.Items.Add(new Separator());
                contextMenu.Items.Add(menuItem);
            }

            var textBlock = this.GetTemplateChild("PART_TextBlock") as TextBlock;
            textBlock.Text = this.name;
            textBlock.Padding = new Thickness(this.padding, 0, 0, 0);
            textBlock.ContextMenu = contextMenu;

            textBox.Text = val?.ToString(CultureInfo.InvariantCulture);
            textBox.ContextMenu = contextMenu;
            this.lastValue = textBox.Text;
            textBox.LostFocus += this.TextBoxUpdated;
        }

        private void TextBoxUpdated(object sender, RoutedEventArgs e)
        {
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
        }

        protected override string GetStringValue()
        {
            return this.GetValue().ToString(CultureInfo.InvariantCulture);
        }

        public abstract IConvertible GetValue();
        public abstract void SetValue(IConvertible value);
    }

    public abstract class BaseBoolItem : BaseItem
    {
        protected string name;
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
        private string lastValue;

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
            var stringVal = val.ToString();
            var type = val.GetType();
            var enumNames = type.GetEnumNames();
            foreach (var enumName in enumNames)
            {
                comboBox.Items.Add(new ComboBoxItem { Content = enumName, IsSelected = stringVal == enumName });
            }

            if (comboBox.SelectedItem == null)
            {
                comboBox.Text = stringVal;
            }

            this.lastValue = comboBox.Text;

            comboBox.LostFocus += (s, e) =>
            {
                var text = comboBox.Text;
                try
                {
                    if (comboBox.Text != this.lastValue)
                    {
                        var selectedItem = comboBox.SelectedItem as ComboBoxItem;
                        if (selectedItem != null)
                        {
                            this.SetValue(Enum.Parse(type, selectedItem.Content as string) as Enum);
                        }
                        else
                        {
                            var intType = Enum.GetUnderlyingType(type);
                            var converter = TypeDescriptor.GetConverter(intType);
                            var result = converter.ConvertFromInvariantString(comboBox.Text);
                            this.SetValue(result as IConvertible);

                            if (Enum.IsDefined(type, result))
                            {
                                comboBox.Text = Enum.GetName(type, result);
                            }
                        }

                        this.GenerateUpdateCommand(comboBox.Text);
                        (this.parent as IParentUpdate)?.Update();
                        this.lastValue = comboBox.Text;
                    }
                }
                catch
                {
                    MessageBox.Show($"{comboBox.Text} is not a valid value for {type}", "Property error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    comboBox.Text = this.lastValue;
                }
            };
        }

        protected override string GetStringValue()
        {
            return this.GetValue().ToString();
        }

        public abstract Enum GetValue();

        public abstract void SetValue(IConvertible val);
    }

    public class VaultNameItem : Control
    {
        private string vaultName;

        public VaultNameItem(string vaultName)
        {
            this.vaultName = vaultName;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var textBox = this.GetTemplateChild("PART_TextBox") as TextBox;
            textBox.Text = this.vaultName;
        }
    }
}
