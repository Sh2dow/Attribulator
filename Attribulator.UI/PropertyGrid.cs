using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VaultLib.Core.Data;
using EAType = VaultLib.Core.Types.EA.Reflection;

namespace Attribulator.UI
{
    public class PropertyGridItem : Grid
    {
        protected VltCollection collection;
        protected string name;

        private string lastValue;
        protected EAType.PrimitiveTypeBase prop;

        public PropertyGridItem(string name, EAType.PrimitiveTypeBase prop, VltCollection collection)
        {
            this.prop = prop;
            this.collection = collection;
            this.name = name;

            this.RowDefinitions.Add(new RowDefinition());

            this.ColumnDefinitions.Add(new ColumnDefinition());
            this.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            this.ColumnDefinitions.Add(new ColumnDefinition());
            this.ColumnDefinitions[1].Width = new GridLength(2, GridUnitType.Star);

            var textBlock = new TextBlock();
            textBlock.SetValue(ColumnProperty, 0);
            textBlock.Text = name;

            this.Children.Add(textBlock);

            this.CreateEditor();
        }

        protected virtual void CreateEditor()
        {
            var val = prop.GetValue();
            var textBox = new TextBox();
            textBox.SetValue(ColumnProperty, 1);
            textBox.Text = val.ToString(System.Globalization.CultureInfo.InvariantCulture);
            textBox.LostFocus += TextBox_TextChanged;
            lastValue = textBox.Text;
            this.Children.Add(textBox);
        }

        private void TextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;

            try
            {
                var result = float.Parse(textBox.Text, System.Globalization.CultureInfo.InvariantCulture);
                this.collection.SetDataValue(this.name, result);
                this.lastValue = textBox.Text;
            }
            catch
            {
                textBox.Text = this.lastValue;
            }
        }
    }

    public class PropertyGridItemBool : PropertyGridItem
    {
        public PropertyGridItemBool(string name, EAType.Bool prop, VltCollection collection) : base(name, prop, collection)
        {
            this.prop = prop;
        }

        protected override void CreateEditor()
        {
            var checkBox = new CheckBox();
            checkBox.SetValue(ColumnProperty, 1);
            checkBox.IsChecked = (prop as EAType.Bool).Value;
            checkBox.Checked += CheckBox_Changed;
            checkBox.Unchecked += CheckBox_Changed;
            this.Children.Add(checkBox);
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            this.collection.SetDataValue(this.name, checkBox.IsChecked);
        }
    }

    public class PropertyGrid : StackPanel
    {
        public void Display(VltCollection collection)
        {
            this.Children.Clear();

            if (collection != null)
            {
                var properties = collection.GetData();

                foreach (var property in properties.OrderBy(x => x.Key))
                {
                    if (property.Value is EAType.Bool)
                    {
                        this.Children.Add(new PropertyGridItemBool(property.Key, property.Value as EAType.Bool, collection));
                    }
                    else if (property.Value is EAType.PrimitiveTypeBase)
                    {
                        this.Children.Add(new PropertyGridItem(property.Key, property.Value as EAType.PrimitiveTypeBase, collection));
                    }
                }
            }
        }
    }
}
