using AttribulatorUI;
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Attribulator.UI.PropertyGrid
{
    public static class GridHelper
    {
        public static Control ResolvePrimitiveItem(IParent parent, string name, VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase prop, int padding)
        {
            if (prop is VaultLib.Core.Types.EA.Reflection.Bool)
            {
                return new PrimitiveBoolItem(parent, name, prop as VaultLib.Core.Types.EA.Reflection.Bool, padding);
            }

            if (prop.GetType().IsGenericType && prop.GetType().GetGenericTypeDefinition() == typeof(VaultLib.Core.Types.VLTEnumType<>))
            {
                return new PrimitiveEnumItem(parent, name, prop, padding);
            }

            return new PrimitiveItem(parent, name, prop, padding);
        }
    }

    public class EditFieldItem : Control
    {
        public string FieldName { get; private set; }

        public bool IsChecked { get; private set; }

        public EditFieldItem(string name, bool isChecked)
        {
            this.FieldName = name;
            this.IsChecked = isChecked;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var nameTextBlock = this.GetTemplateChild("PART_Name") as TextBlock;
            nameTextBlock.Text = this.FieldName;

            var checkbox = this.GetTemplateChild("PART_Checkbox") as CheckBox;
            checkbox.IsChecked = this.IsChecked;
            checkbox.Checked += (s, e) => this.IsChecked = true;
            checkbox.Unchecked += (s, e) => this.IsChecked = false;
        }
    }

    public class ClassItem : CollapseItem, ICommandName
    {
        private IParent parent;
        private string name;

        public ClassItem(IParent parent, string name, VaultLib.Core.Types.VLTBaseType prop, int padding) : base(prop, name, prop.ToString(), padding)
        {
            this.parent = parent;
            this.name = name;

            var props = prop.GetType().GetProperties().OrderBy(x => x.Name).ToList();
            for (int i = 0; i < props.Count; i++)
            {
                var pi = props[i];
                var type = pi.PropertyType;
                int subPadding = padding + 41;
                if (prop is VaultLib.Core.Types.Attrib.Types.Matrix)
                {
                    this.AddChild(new MatrixItem(this, prop as VaultLib.Core.Types.Attrib.Types.Matrix, padding + 21));
                }
                else if (type.IsSubclassOf(typeof(VaultLib.Core.Types.VLTBaseType)))
                {
                    this.AddChild(new ClassItem(this, pi.Name, pi.GetValue(prop) as VaultLib.Core.Types.VLTBaseType, padding + 21));
                }
                else if (type == typeof(bool))
                {
                    this.AddChild(new PropertyBoolItem(this, pi, prop, subPadding));
                }
                else if (type.IsEnum)
                {
                    this.AddChild(new PropertyEnumItem(this, pi, prop, subPadding));
                }
                else if (type.IsArray || type.GetInterfaces().Contains(typeof(IList)))
                {
                    var array = pi.GetValue(prop) as IList;
                    int maxCount = array.Count;
                    var propType = prop.GetType();
                    if (propType.IsGenericType)
                    {
                        var genericType = propType.GetGenericTypeDefinition();
                        if (genericType == typeof(VaultLib.Core.Types.DynamicSizeArray<>) ||
                            genericType == typeof(VaultLib.Core.Types.VLTListContainer<>))
                        {
                            maxCount = int.MaxValue;
                        }
                    }

                    this.AddChild(new PropertyArrayItem(this, pi, prop, maxCount, subPadding));
                }
                else
                {
                    this.AddChild(new PropertyItem(this, pi, prop, subPadding));
                }
            }
        }

        public string GetName()
        {
            string name = "";
            if (this.parent is ICommandName icm)
            {
                name = $"{icm.GetName()} ";
            }

            return name + this.name;
        }

        public override void Update()
        {
            base.Update();
            if (this.parent != null)
            {
                (this.parent as IParentUpdate)?.Update();
            }
        }
    }

    public class ArrayItem : ArrayCollapseItem, ICommandName, IItemAddRemove
    {
        private int padding;
        private ICommandName parent;
        private string name;
        private VaultLib.Core.Types.VLTArrayType prop;
        private int maxCount;

        public ArrayItem(ICommandName parent, string name, VaultLib.Core.Types.VLTArrayType prop, int maxCount, int padding) : base(prop, name, prop.ToString(), padding)
        {
            this.name = name;
            this.parent = parent;
            this.prop = prop;

            this.maxCount = maxCount;

            this.Draw();
        }

        public string GetName()
        {
            return $"{this.parent.GetName()} {this.name}";
        }

        public void AddItem()
        {
            if (this.CanAdd())
            {
                this.Resize(this.prop.Items.Count + 1);
            }
        }

        public void RemoveItem()
        {
            if (this.CanRemove())
            {
                this.Resize(this.prop.Items.Count - 1);
            }
        }

        private void Resize(int size)
        {
            var command = $"resize_field {this.parent.GetName()} {this.name} {size}";
            MainWindow.Instance.ExecuteScriptInternal(new[] { command });
            MainWindow.Instance.AddScriptLine(command);
            this.Draw();
        }

        private void Draw()
        {
            this.ClearChildren();
            for (int i = 0; i < prop.Items.Count; i++)
            {
                string itemName = $"[{i}]";
                if (prop.ItemType.IsSubclassOf(typeof(VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase)))
                {
                    var primitive = prop.Items[i] as VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase;
                    this.AddChild(GridHelper.ResolvePrimitiveItem(this, itemName, primitive, this.padding + 21));
                }
                else
                {
                    this.AddChild(new ClassItem(this, itemName, prop.Items[i], this.padding + 21));
                }
            }
        }

        public bool CanAdd()
        {
            return this.prop.Items.Count < this.maxCount;
        }

        public bool CanRemove()
        {
            return this.prop.Items.Count > 0;
        }
    }

    public class MainGrid : Control, ICommandName, ICommandGenerator
    {
        public VaultLib.Core.Data.VltCollection Collection { get; private set; }
        private StackPanel stackPanel;

        public MainGrid(VaultLib.Core.Data.VltCollection collection)
        {
            this.Collection = collection;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.stackPanel = this.GetTemplateChild("PART_StackPanel") as StackPanel;
            this.Draw();
        }

        public void Draw()
        {
            this.stackPanel.Children.Clear();

            if (Collection != null)
            {
                var properties = Collection.GetData().OrderBy(x => x.Key);
                this.stackPanel.Children.Add(new VaultNameItem(Collection.Vault.Name));
                foreach (var property in properties)
                {
                    var type = property.Value;
                    UIElement child = null;
                    if (type is VaultLib.Core.Types.VLTArrayType)
                    {
                        var field = Collection.Class.FindField(property.Key);
                        var maxCount = field.IsInLayout ? field.MaxCount : int.MaxValue;
                        child = new ArrayItem(this, property.Key, type as VaultLib.Core.Types.VLTArrayType, maxCount, 0);
                    }
                    else if (type is VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase)
                    {
                        child = GridHelper.ResolvePrimitiveItem(this, property.Key, type as VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase, 21);
                    }
                    else if (type is VaultLib.Core.Types.Attrib.BaseBlob)
                    {
                        child = new BlobItem(property.Key, type as VaultLib.Core.Types.Attrib.BaseBlob, 21);
                    }
                    else if (type is VaultLib.Core.Types.VLTBaseType)
                    {
                        child = new ClassItem(this, property.Key, type, 0);
                    }

                    if (child != null)
                    {
                        this.stackPanel.Children.Add(child);
                    }
                    else
                    {
                        Debugger.Break();
                    }
                }
            }
        }

        public void GenerateUpdateCommand()
        {
            foreach (var child in this.stackPanel.Children)
            {
                if (child is ICommandGenerator item)
                {
                    item.GenerateUpdateCommand();
                }
            }
        }

        public string GetName()
        {
            return $"{this.Collection.Class.Name} {this.Collection.Name}";
        }
    }
}
