using AttribulatorUI;
using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VaultLib.Core.Hashing;

namespace Attribulator.UI.PropertyGrid
{
    public static class GridHelper
    {
        public static Control ResolvePrimitiveItem(IParent parent, string name, Func<IConvertible> getValue,
            Action<IConvertible> setValue, int padding)
        {
            var type = getValue().GetType();
            if (type == typeof(bool))
                return new PrimitiveBoolItem(parent, name, () => (bool)getValue(), v => setValue(v), padding);

            if (type.IsEnum)
                return new PrimitiveEnumItem(parent, name, () => (Enum)getValue(), setValue, padding);

            return new PrimitiveItem(parent, name, getValue, setValue, padding);
        }

        public static string ResolveName(Key32 key)
        {
            return HashManager.ResolveVlt(key.Hash) ?? key.ToString();
        }
    }

    public class ClassItem : CollapseItem, ICommandName
    {
        private IParent parent;
        private string name;

        public ClassItem(IParent parent, string name, VLTBaseType prop, int padding) : base(prop, name, prop.ToString(), padding)
        {
            this.parent = parent;
            this.name = name;

            var props = prop.GetType().GetProperties().OrderBy(x => x.Name).ToList();
            for (int i = 0; i < props.Count; i++)
            {
                var pi = props[i];
                var type = pi.PropertyType;
                int subPadding = padding + 41;
                if (type == typeof(VaultLib.Core.Types.Attrib.Types.Matrix))
                {
                    this.AddChild(new MatrixItem(this, prop, pi, padding + 21));
                }
                else if (type.IsSubclassOf(typeof(VLTBaseType)))
                {
                    this.AddChild(new ClassItem(this, pi.Name, pi.GetValue(prop) as VLTBaseType, padding + 21));
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
                        if (genericType == typeof(VaultLib.Core.Types.DynamicSizeArray<,>) ||
                            genericType == typeof(VaultLib.Core.Types.VltListContainer<,>))
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
        private VLTArrayType prop;
        private int maxCount;

        public ArrayItem(ICommandName parent, string name, VLTArrayType prop, int maxCount, int padding) : base(prop, name, prop.ToString(), padding)
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
                var item = prop.Items[i];
                if (item is VLTBaseType)
                {
                    this.AddChild(new ClassItem(this, itemName, (VLTBaseType)item, this.padding + 21));
                    continue;
                }

                this.AddChild(GridHelper.ResolvePrimitiveItem(
                    this,
                    itemName,
                    () => (IConvertible)prop.Items[i],
                    v => prop.SetValue(i, v),
                    this.padding + 21));
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
        public VltCollection Collection { get; private set; }
        private StackPanel stackPanel;
        private StackPanel searchResults;
        private TextBlock searchHeader;

        public MainGrid(VltCollection collection)
        {
            this.Collection = collection;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.stackPanel = this.GetTemplateChild("PART_StackPanel") as StackPanel;
            this.searchResults = this.GetTemplateChild("PART_SearchResults") as StackPanel;
            this.searchHeader = this.GetTemplateChild("PART_SearchHeader") as TextBlock;
            this.Draw();
        }

        public void Draw()
        {
            this.stackPanel.Children.Clear();

            this.searchResults.Children.Clear();
            this.searchResults.Visibility = Visibility.Collapsed;
            this.searchHeader.Visibility = Visibility.Collapsed;

            if (Collection != null)
            {
                var properties = Collection.GetData().OrderBy(x => x.Key);
                this.stackPanel.Children.Add(new VaultNameItem(Collection.Vault.Name));
                foreach (var property in properties)
                {
                    string name = GridHelper.ResolveName(property.Key);
                    var type = property.Value;
                    UIElement child = null;
                    if (type is VLTArrayType)
                    {
                        var field = Collection.Class.FindField(property.Key);
                        var maxCount = field.IsInLayout ? field.MaxCount : int.MaxValue;
                        child = new ArrayItem(this, name, type as VLTArrayType, maxCount, 0);
                    }
                    else if (type is IConvertible)
                    {
                        child = GridHelper.ResolvePrimitiveItem(
                            this,
                            name,
                            () => (IConvertible)Collection.GetRawValue(property.Key),
                            v => Collection.SetRawValue(property.Key, v),
                            21);
                    }
                    else if (type is BaseBlob)
                    {
                        child = new BlobItem(name, type as BaseBlob, 21);
                    }
                    else if (type is VLTBaseType)
                    {
                        child = new ClassItem(this, name, (VLTBaseType)type, 0);
                    }

                    if (child != null)
                    {
                        if (this.IsSearchitem(name, type))
                        {
                            this.searchResults.Children.Add(child);
                            this.searchResults.Visibility = Visibility.Visible;
                            this.searchHeader.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            this.stackPanel.Children.Add(child);
                        }
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
            return $"{GridHelper.ResolveName(this.Collection.Class.Key)} {GridHelper.ResolveName(this.Collection.Key)}";
        }

        private bool IsSearchitem(string name, object type)
        {
            if (MainWindow.Instance.Search.Executed)
            {
                var searchSettings = MainWindow.Instance.Search.Settings;
                if (searchSettings.FieldEnabled || searchSettings.ValueEnabled)
                {
                    if (searchSettings.FieldEnabled)
                    {
                        if (!name.Contains(searchSettings.FieldText, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return false;
                        }
                    }

                    if (searchSettings.ValueEnabled)
                    {
                        if (!type.ToString().Contains(searchSettings.ValueText, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            return false;
        }
    }
}
