using System;
using System.Collections;
using System.Reflection;
using AttribulatorUI;

namespace Attribulator.UI.PropertyGrid
{
    public class PropertyBoolItem : BaseBoolItem, ICommandName
    {
        private VaultLib.Core.Types.VLTBaseType prop;
        private PropertyInfo propertyInfo;

        public PropertyBoolItem(IParent parent, PropertyInfo propertyInfo, VaultLib.Core.Types.VLTBaseType prop, int padding) : base(parent, propertyInfo.Name, padding)
        {
            this.prop = prop;
            this.propertyInfo = propertyInfo;
        }

        public string GetName()
        {
            return this.propertyInfo.Name;
        }

        public override bool GetValue()
        {
            return (bool)this.propertyInfo.GetValue(this.prop);
        }

        public override void SetValue(bool value)
        {
            this.propertyInfo.SetValue(this.prop, value);
        }
    }

    public class PropertyEnumItem : BaseEnumItem, ICommandName
    {
        private VaultLib.Core.Types.VLTBaseType prop;
        private PropertyInfo propertyInfo;

        public PropertyEnumItem(IParent parent, PropertyInfo propertyInfo, VaultLib.Core.Types.VLTBaseType prop, int padding) : base(parent, propertyInfo.Name, padding)
        {
            this.prop = prop;
            this.propertyInfo = propertyInfo;
        }

        public string GetName()
        {
            return this.propertyInfo.Name;
        }

        public override Enum GetValue()
        {
            return (Enum)this.propertyInfo.GetValue(this.prop);
        }

        public override void SetValue(IConvertible value)
        {
            this.propertyInfo.SetValue(this.prop, value);
        }
    }

    public class PropertyItem : BaseEditItem, ICommandName
    {
        private VaultLib.Core.Types.VLTBaseType prop;
        private PropertyInfo propertyInfo;

        public PropertyItem(IParent parent, PropertyInfo propertyInfo, VaultLib.Core.Types.VLTBaseType prop, int padding) : base(parent, propertyInfo.Name, padding)
        {
            this.prop = prop;
            this.propertyInfo = propertyInfo;
        }

        public string GetName()
        {
            return this.propertyInfo.Name;
        }

        public override IConvertible GetValue()
        {
            return this.propertyInfo.GetValue(this.prop) as IConvertible;
        }

        public override void SetValue(IConvertible value)
        {
            this.propertyInfo.SetValue(this.prop, value);
        }
    }

    public class PropertyArraySubItem : BaseEditItem, ICommandName
    {
        private int index;
        private IList array;

        public PropertyArraySubItem(IParent parent, IList array, int index, string name, int padding) : base(parent, name, padding)
        {
            this.index = index;
            this.array = array;
        }

        public string GetName()
        {
            return this.name;
        }

        public override IConvertible GetValue()
        {
            return this.array[this.index] as IConvertible;
        }

        public override void SetValue(IConvertible value)
        {
            this.array[this.index] = value;
        }
    }

    public class PropertyArrayItem : ArrayCollapseItem, IItemAddRemove, ICommandName
    {
        private VaultLib.Core.Types.VLTBaseType prop;
        private PropertyInfo propertyInfo;
        private int padding;
        private IParent parent;
        private int maxCount;
        private IList array;

        public PropertyArrayItem(IParent parent, PropertyInfo propertyInfo, VaultLib.Core.Types.VLTBaseType prop, int maxCount, int padding) : base(prop, propertyInfo.Name, prop.ToString(), padding)
        {
            this.prop = prop;
            this.propertyInfo = propertyInfo;
            this.padding = padding;
            this.parent = parent;
            this.maxCount = maxCount;
            this.array = this.propertyInfo.GetValue(this.prop) as IList;

            this.Draw();
        }

        private void Draw()
        {
            this.ClearChildren();
            var array = this.propertyInfo.GetValue(this.prop) as IList;
            for (int i = 0; i < array.Count; i++)
            {
                var type = array[i].GetType();
                if (type.IsSubclassOf(typeof(VaultLib.Core.Types.VLTBaseType)))
                {
                    this.AddChild(new ClassItem(this, $"[{i}]", array[i] as VaultLib.Core.Types.VLTBaseType, padding + 21));
                }
                else
                {
                    this.AddChild(new PropertyArraySubItem(parent, array, i, $"[{i}]", padding + 21));
                }
            }
        }

        public void AddItem()
        {
            if (this.CanAdd())
            {
                this.Resize(this.array.Count + 1);
            }
        }

        public void RemoveItem()
        {
            if (this.CanRemove())
            {
                this.Resize(this.array.Count - 1);
            }
        }

        private void Resize(int size)
        {
            var command = $"resize_collection {(this.parent as ICommandName).GetName()} {this.propertyInfo.Name} {size}";
            command = command.Replace(" [", "[");
            MainWindow.Instance.ExecuteScriptInternal(new[] { command });
            MainWindow.Instance.AddScriptLine(command);
            this.Draw();
        }

        public bool CanAdd()
        {
            return this.array.Count < this.maxCount;
        }

        public bool CanRemove()
        {
            return this.array.Count > 0;
        }

        public string GetName()
        {
            string name = "";
            if (this.parent is ICommandName icm)
            {
                name = $"{icm.GetName()} ";
            }

            return name + this.propertyInfo.Name;
        }
    }

    public class MatrixItem : CollapseItem, ICommandName
    {
        private IParent parent;

        public MatrixItem(IParent parent, VaultLib.Core.Types.Attrib.Types.Matrix prop, int padding) : base(prop, "Data", prop.ToString(), padding)
        {
            this.parent = parent;
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    this.AddChild(new PropertyArraySubItem(this, prop.Data, 4 * i + j, $"[{i + 1},{j + 1}]", padding + 21));
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

            return name + "Data";
        }
    }
}
