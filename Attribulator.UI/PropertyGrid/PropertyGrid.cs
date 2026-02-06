using AttribulatorUI;
using System;
using System.ComponentModel;
using System.Collections;
using System.Reflection;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VaultLib.Core.Hashing;

namespace Attribulator.UI.PropertyGrid
{
    public static class GridHelper
    {
        public static Control ResolvePrimitiveItem(IParent parent, string name, Func<object> getValue,
            Action<object> setValue, int padding)
        {
            var type = getValue().GetType();
            if (type == typeof(bool))
                return new PrimitiveBoolItem(parent, name, () => (bool)getValue(), v => setValue(v), padding);

            if (type.IsEnum)
                return new PrimitiveEnumItem(parent, name, () => (Enum)getValue(), setValue, padding);

            return new PrimitiveItem(parent, name, getValue, setValue, padding);
        }

        public static bool IsEditablePrimitive(Type type)
        {
            if (type == null)
                return false;

            if (type.IsEnum || type == typeof(bool))
                return true;

            if (typeof(IConvertible).IsAssignableFrom(type))
                return true;

            if (ImplementsGenericInterface(type, typeof(VaultLib.Core.DataInterfaces.IKey<>)))
                return true;

            var underlying = Nullable.GetUnderlyingType(type);
            if (underlying != null)
                return IsEditablePrimitive(underlying);

            var converter = TypeDescriptor.GetConverter(type);
            return converter != null &&
                   converter.CanConvertFrom(typeof(string)) &&
                   converter.CanConvertTo(typeof(string));
        }

        public static bool IsStructWithPublicMembers(Type type)
        {
            if (type == null)
                return false;

            if (!type.IsValueType || type.IsEnum || type.IsPrimitive)
                return false;

            if (typeof(IConvertible).IsAssignableFrom(type))
                return false;

            if (ImplementsGenericInterface(type, typeof(VaultLib.Core.DataInterfaces.IKey<>)))
                return false;

            if (type.GetFields(BindingFlags.Public | BindingFlags.Instance).Length > 0)
                return true;

            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Any(p => p.CanRead);
        }

        private static bool ImplementsGenericInterface(Type type, Type genericInterface)
        {
            return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericInterface);
        }

        public static string ResolveName(Key32 key)
        {
            return HashManager.ResolveVlt(key.Hash) ?? key.ToString();
        }

        public static string FormatValue(object value)
        {
            if (value == null)
                return string.Empty;

            if (value is VLTArrayType array)
            {
                if (array.Items.Count == 0)
                    return "[]";

                // Use first element to provide a meaningful summary (matches previous UI behavior).
                var first = array.Items[0];
                return FormatValue(first);
            }

            var keyString = TryFormatKey(value);
            if (keyString != null)
                return keyString;

            if (TryFormatRefSpec(value, out var refSpecString))
                return refSpecString;

            return value.ToString();
        }

        private static string? TryFormatKey(object value)
        {
            var valueType = value.GetType();
            var keyInterface = valueType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(VaultLib.Core.DataInterfaces.IKey<>));
            if (keyInterface == null)
                return null;

            var keyType = keyInterface.GetGenericArguments()[0];
            var method = typeof(Attribulator.API.Utils.KeyUtils).GetMethod(nameof(Attribulator.API.Utils.KeyUtils.KeyToString))
                ?.MakeGenericMethod(keyType);
            if (method == null)
                return null;

            return method.Invoke(null, new[] { value }) as string;
        }

        private static bool TryFormatRefSpec(object value, out string formatted)
        {
            formatted = string.Empty;

            var type = value.GetType();
            var classKeyProp = type.GetProperty("ClassKey");
            var collectionKeyProp = type.GetProperty("CollectionKey");

            if (classKeyProp == null || collectionKeyProp == null)
                return false;

            var classKey = classKeyProp.GetValue(value);
            var collectionKey = collectionKeyProp.GetValue(value);
            if (classKey == null || collectionKey == null)
                return false;

            var classKeyStr = TryFormatKey(classKey) ?? classKey.ToString();
            var collectionKeyStr = TryFormatKey(collectionKey) ?? collectionKey.ToString();
            formatted = $"{classKeyStr} -> {collectionKeyStr}";
            return true;
        }
    }

    public class ClassItem : CollapseItem, ICommandName
    {
        private IParent parent;
        private string name;

        public ClassItem(IParent parent, string name, VLTBaseType prop, int padding) : base(prop, name, GridHelper.FormatValue(prop), padding)
        {
            this.parent = parent;
            this.name = name;

            var propType = prop.GetType();
            var props = propType.GetProperties()
                .Where(pi => ShouldDisplayProperty(propType, pi))
                .OrderBy(x => x.Name)
                .ToList();
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
                else if (GridHelper.IsStructWithPublicMembers(type))
                {
                    this.AddChild(new StructItem(this, pi, prop, subPadding));
                }
                else if (type.IsArray || type.GetInterfaces().Contains(typeof(IList)))
                {
                    var array = pi.GetValue(prop) as IList;
                    int maxCount = array.Count;
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
                else if (GridHelper.IsEditablePrimitive(type))
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

        private static bool ShouldDisplayProperty(Type ownerType, PropertyInfo propertyInfo)
        {
            if (ownerType == null || propertyInfo == null)
                return false;

            var ownerNs = ownerType.Namespace ?? string.Empty;
            var declaringNs = propertyInfo.DeclaringType?.Namespace ?? string.Empty;

            // For non-core types, hide properties inherited from core container types
            if (!ownerNs.StartsWith("VaultLib.Core.Types", StringComparison.Ordinal) &&
                declaringNs.StartsWith("VaultLib.Core.Types", StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }

    }

    public class StructItem : CollapseItem, ICommandName
    {
        private readonly IParent parent;
        private readonly string name;
        private readonly PropertyInfo propertyInfo;
        private readonly VLTBaseType owner;

        public StructItem(IParent parent, PropertyInfo propertyInfo, VLTBaseType owner, int padding)
            : base(propertyInfo.GetValue(owner), propertyInfo.Name, GridHelper.FormatValue(propertyInfo.GetValue(owner)), padding)
        {
            this.parent = parent;
            this.name = propertyInfo.Name;
            this.propertyInfo = propertyInfo;
            this.owner = owner;

            this.BuildFields(padding + 21);
        }

        public string GetName()
        {
            string namePrefix = "";
            if (this.parent is ICommandName icm)
            {
                namePrefix = $"{icm.GetName()} ";
            }

            return namePrefix + this.name;
        }

        private object GetStruct()
        {
            return this.propertyInfo.GetValue(this.owner);
        }

        private void SetStruct(object value)
        {
            this.propertyInfo.SetValue(this.owner, value);
        }

        private void BuildFields(int padding)
        {
            var structType = this.propertyInfo.PropertyType;
            var fields = structType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(f => f.Name)
                .ToList();

            foreach (var field in fields)
            {
                var fieldName = field.Name;
                var fieldType = field.FieldType;

                if (!GridHelper.IsEditablePrimitive(fieldType) && !fieldType.IsEnum && fieldType != typeof(bool))
                {
                    continue;
                }

                this.AddChild(GridHelper.ResolvePrimitiveItem(
                    this,
                    fieldName,
                    () =>
                    {
                        var boxed = GetStruct();
                        return field.GetValue(boxed);
                    },
                    v =>
                    {
                        var boxed = GetStruct();
                        field.SetValue(boxed, v);
                        SetStruct(boxed);
                    },
                    padding));
            }

            var props = structType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
                .OrderBy(p => p.Name)
                .ToList();

            foreach (var prop in props)
            {
                var propType = prop.PropertyType;
                if (!GridHelper.IsEditablePrimitive(propType) && !propType.IsEnum && propType != typeof(bool))
                {
                    continue;
                }

                this.AddChild(GridHelper.ResolvePrimitiveItem(
                    this,
                    prop.Name,
                    () =>
                    {
                        var boxed = GetStruct();
                        return prop.GetValue(boxed);
                    },
                    v =>
                    {
                        var boxed = GetStruct();
                        prop.SetValue(boxed, v);
                        SetStruct(boxed);
                    },
                    padding));
            }
            this.SortChildren();
        }
    }

    public class StructValueItem : CollapseItem, ICommandName
    {
        private readonly IParent parent;
        private readonly string name;
        private readonly Func<object> getValue;
        private readonly Action<object> setValue;

        public StructValueItem(IParent parent, string name, Func<object> getValue, Action<object> setValue, int padding)
            : base(getValue(), name, GridHelper.FormatValue(getValue()), padding)
        {
            this.parent = parent;
            this.name = name;
            this.getValue = getValue;
            this.setValue = setValue;

            this.BuildFields(padding + 21);
        }

        public string GetName()
        {
            string namePrefix = "";
            if (this.parent is ICommandName icm)
            {
                namePrefix = $"{icm.GetName()} ";
            }

            return namePrefix + this.name;
        }

        private void BuildFields(int padding)
        {
            var structType = this.getValue().GetType();
            var fields = structType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(f => f.Name)
                .ToList();

            foreach (var field in fields)
            {
                var fieldType = field.FieldType;
                if (!GridHelper.IsEditablePrimitive(fieldType) && !fieldType.IsEnum && fieldType != typeof(bool))
                {
                    continue;
                }

                this.AddChild(GridHelper.ResolvePrimitiveItem(
                    this,
                    field.Name,
                    () =>
                    {
                        var boxed = this.getValue();
                        return field.GetValue(boxed);
                    },
                    v =>
                    {
                        var boxed = this.getValue();
                        field.SetValue(boxed, v);
                        this.setValue(boxed);
                    },
                    padding));
            }

            var props = structType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
                .OrderBy(p => p.Name)
                .ToList();

            foreach (var prop in props)
            {
                var propType = prop.PropertyType;
                if (!GridHelper.IsEditablePrimitive(propType) && !propType.IsEnum && propType != typeof(bool))
                {
                    continue;
                }

                this.AddChild(GridHelper.ResolvePrimitiveItem(
                    this,
                    prop.Name,
                    () =>
                    {
                        var boxed = this.getValue();
                        return prop.GetValue(boxed);
                    },
                    v =>
                    {
                        var boxed = this.getValue();
                        prop.SetValue(boxed, v);
                        this.setValue(boxed);
                    },
                    padding));
            }
            this.SortChildren();
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

                var index = i;
                var itemType = item?.GetType();
                if (itemType != null && GridHelper.IsStructWithPublicMembers(itemType))
                {
                    this.AddChild(new StructValueItem(
                        this,
                        itemName,
                        () => index >= 0 && index < prop.Items.Count ? prop.Items[index] : null,
                        v =>
                        {
                            if (index >= 0 && index < prop.Items.Count)
                            {
                                prop.SetValue(index, v);
                            }
                        },
                        this.padding));
                }
                else
                {
                    this.AddChild(GridHelper.ResolvePrimitiveItem(
                        this,
                        itemName,
                        () => index >= 0 && index < prop.Items.Count ? prop.Items[index] : null,
                        v =>
                        {
                            if (index >= 0 && index < prop.Items.Count)
                            {
                                prop.SetValue(index, v);
                            }
                        },
                        this.padding + 21));
                }
            }
            this.SortChildren();
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
                var properties = Collection.GetData()
                    .Where(x => Collection.Class.HasField(x.Key))
                    .OrderBy(x => GridHelper.ResolveName(x.Key), StringComparer.InvariantCultureIgnoreCase);
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
                    else if (GridHelper.IsStructWithPublicMembers(type.GetType()))
                    {
                        child = new StructValueItem(
                            this,
                            name,
                            () => Collection.GetRawValue(property.Key),
                            v => Collection.SetRawValue(property.Key, v),
                            0);
                    }
                    else if (GridHelper.IsEditablePrimitive(type.GetType()))
                    {
                        child = GridHelper.ResolvePrimitiveItem(
                            this,
                            name,
                            () => Collection.GetRawValue(property.Key),
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
