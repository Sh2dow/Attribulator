using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VaultLib.Core.Data;

namespace Attribulator.UI.PropertyGrid
{
    public class ClassItem : CollapseItem
    {
        private IParent parent;

        public ClassItem(IParent parent, string name, VaultLib.Core.Types.VLTBaseType prop, int padding) : base(prop, name, prop.ToString(), padding)
        {
            this.parent = parent;

            var props = prop.GetType().GetProperties().OrderBy(x => x.Name).ToList();
            for (int i = 0; i < props.Count; i++)
            {
                var pi = props[i];
                var type = pi.PropertyType;
                int subPadding = padding + 41;
                if (type.IsSubclassOf(typeof(VaultLib.Core.Types.VLTBaseType)))
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
                else if (type.IsArray)
                {

                }
                else
                {
                    this.AddChild(new PropertyItem(this, pi, prop, subPadding));
                }
            }
        }

        public override void Update()
        {
            base.Update();
            if (this.parent != null)
            {
                this.parent.Update();
            }
        }
    }

    public class ArrayItem : CollapseItem
    {
        private int padding;

        public ArrayItem(string name, VaultLib.Core.Types.VLTArrayType prop, int padding) : base(prop, name, prop.ToString(), padding)
        {
            for (int i = 0; i < prop.Items.Count; i++)
            {
                string itemName = $"[{i}]";
                if (prop.ItemType.IsSubclassOf(typeof(VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase)))
                {
                    var primitive = prop.Items[i] as VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase;
                    this.AddChild(MainGrid.ResolvePrimitiveItem(this, itemName, primitive, this.padding + 21));
                }
                else
                {
                    this.AddChild(new ClassItem(this, itemName, prop.Items[i], this.padding + 21));
                }
            }
        }
    }

    public class MainGrid : StackPanel
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
                        child = new ArrayItem(property.Key, type as VaultLib.Core.Types.VLTArrayType, 0);
                    }
                    else if (type is VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase)
                    {
                        child = ResolvePrimitiveItem(null, property.Key, type as VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase, 21);
                    }
                    else if (type is VaultLib.Core.Types.VLTBaseType)
                    {
                        child = new ClassItem(null, property.Key, type, 0);
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
