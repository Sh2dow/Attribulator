using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Xml.Linq;
using VaultLib.Core.Data;

namespace Attribulator.UI.PropertyGrid
{
    public interface IExpandCollapse
    {
        void Expand();

        void Collapse();
    }

    public class CollapseHeader : Control
    {
        private string name;

        private string value;

        private TextBlock valueTextBlock;

        private ToggleButton toggleButton;

        private IExpandCollapse parent;

        public CollapseHeader(IExpandCollapse parent, string headerName, string value)
        {
            this.name = headerName;
            this.value = value;
            this.parent = parent;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.toggleButton = GetTemplateChild("PART_ItemToggler") as ToggleButton;
            this.toggleButton.Checked += (s, e) => this.parent.Expand();
            this.toggleButton.Unchecked += (s, e) => this.parent.Collapse();

            var headerText = GetTemplateChild("PART_HeaderText") as TextBlock;
            headerText.Text = this.name;

            this.valueTextBlock = GetTemplateChild("PART_ValueText") as TextBlock;
            this.valueTextBlock.Text = this.value;
        }

        public void UpdateValueText(string value)
        {
            this.value = value;
            this.valueTextBlock.Text = this.value;
        }
    }

    public interface IParent
    {
        void Update();
    }

    public class PrimitiveItem : Control
    {
        private string name;
        private VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase prop;
        private TextBox textBox;
        private string lastValue;
        private IParent parent;

        public PrimitiveItem(IParent parent, string name, VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase prop)
        {
            this.name = name;
            this.prop = prop;
            this.parent = parent;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var headerText = GetTemplateChild("PART_TextBlock") as TextBlock;
            headerText.Text = this.name;

            this.textBox = GetTemplateChild("PART_TextBox") as TextBox;
            var val = this.prop.GetValue();
            this.textBox.Text = val.ToString(System.Globalization.CultureInfo.InvariantCulture);
            this.lastValue = this.textBox.Text;
            this.textBox.LostFocus += (s, e) =>
            {
                var textBox = s as TextBox;
                var type = this.prop.GetValue().GetType();
                try
                {
                    var converter = TypeDescriptor.GetConverter(type);
                    var result = converter.ConvertFromInvariantString(textBox.Text);
                    this.prop.SetValue(result as IConvertible);
                    this.lastValue = textBox.Text;
                    if (this.parent != null)
                    {
                        this.parent.Update();
                    }
                }
                catch
                {
                    MessageBox.Show($"{textBox.Text} is not a valid value for {type}", "Property error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    textBox.Text = this.lastValue;
                }
            };
        }
    }

    public class CollapseItem : StackPanel, IExpandCollapse, IParent
    {
        private CollapseHeader headerItem;
        private StackPanel collapsePanel;
        private object prop;

        public CollapseItem(object prop, string name, string value)
        {
            this.prop = prop;
            this.collapsePanel = new StackPanel();
            this.headerItem = new CollapseHeader(this, name, value);

            this.Children.Add(this.headerItem);
            this.Children.Add(this.collapsePanel);

            this.Collapse();
        }

        protected void AddChild(UIElement child)
        {
            this.collapsePanel.Children.Add(child);
        }

        public void Expand()
        {
            this.collapsePanel.Visibility = Visibility.Visible;
        }

        public void Collapse()
        {
            this.collapsePanel.Visibility = Visibility.Collapsed;
        }

        public void Update()
        {
            this.headerItem.UpdateValueText(this.prop.ToString());
        }
    }

    public class ClassItem : CollapseItem
    {
        private CollapseHeader headerItem;
        private IParent parent;

        public ClassItem(IParent parent, string name, VaultLib.Core.Types.VLTBaseType prop) : base(prop, name, prop.ToString())
        {
            this.headerItem = new CollapseHeader(this, name, prop.ToString());

            var props = prop.GetType().GetProperties();
            for (int i = 0; i < props.Length; i++)
            {
                var property = props[i];
                this.AddChild(new TextBlock { Text = property.Name });
            }
        }
    }

    public class ArrayItem : CollapseItem
    {
        public ArrayItem(string name, VaultLib.Core.Types.VLTArrayType prop) : base(prop, name, prop.ToString())
        {
            for (int i = 0; i < prop.Items.Count; i++)
            {
                string itemName = $"[{i}]";
                if (prop.ItemType.IsSubclassOf(typeof(VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase)))
                {
                    var primitive = prop.Items[i] as VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase;
                    this.AddChild(new PrimitiveItem(this, itemName, primitive));
                }
                else
                {
                    this.AddChild(new ClassItem(this, itemName, prop.Items[i]));
                }
            }
        }
    }

    public class MainGrid : StackPanel
    {
        public void Display(VltCollection collection)
        {
            this.Children.Clear();

            if (collection != null)
            {
                var properties = collection.GetData().OrderBy(x => x.Key);

                foreach (var property in properties)
                {
                    UIElement child = null;
                    var type = property.Value;
                    if (type is VaultLib.Core.Types.VLTArrayType)
                    {
                        child = new ArrayItem(property.Key, type as VaultLib.Core.Types.VLTArrayType);
                    }
                    else if (type is VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase)
                    {
                        child = new PrimitiveItem(null, property.Key, type as VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase);
                    }
                    else if (type is VaultLib.Core.Types.VLTBaseType)
                    {
                        child = new ClassItem(null, property.Key, type);
                    }

                    if (child != null)
                    {
                        this.Children.Add(child);
                    }
                    else
                    {
                        Debugger.Break();
                    }
                }
            }
        }
    }
}
