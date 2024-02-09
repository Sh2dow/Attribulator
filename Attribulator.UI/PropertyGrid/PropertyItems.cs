using System;
using System.Collections;
using System.Reflection;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using System.Windows.Forms;
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

        public override void SetValue(Enum value)
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

    public class PropertyArraySubItem : BaseEditItem
    {
        private int index;
        private IList array;

        public PropertyArraySubItem(IParent parent, IList array, int index, int padding) : base(parent, $"[{index}]", padding)
        {
            this.index = index;
            this.array = array;
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

    public class PropertyArrayItem : ArrayCollapseItem, IItemAddRemove
    {
        private VaultLib.Core.Types.VLTBaseType prop;
        private PropertyInfo propertyInfo;
        private int padding;
        private IParent parent;
        private int maxCount;

        public PropertyArrayItem(IParent parent, PropertyInfo propertyInfo, VaultLib.Core.Types.VLTBaseType prop, int maxCount, int padding) : base(prop, propertyInfo.Name, prop.ToString(), padding)
        {
            this.prop = prop;
            this.propertyInfo = propertyInfo;
            this.padding = padding;
            this.parent = parent;
            this.maxCount = maxCount;

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
                    this.AddChild(new PropertyArraySubItem(parent, array, i, padding + 21));
                }
            }
        }

        public void AddItem()
        {
            var array = this.propertyInfo.GetValue(this.prop) as IList;
            if (array.Count < this.maxCount)
            {
                this.Resize(array.Count + 1);
            }
        }

        public void RemoveItem()
        {
            var array = this.propertyInfo.GetValue(this.prop) as IList;
            if (array.Count > 0)
            {
                this.Resize(array.Count - 1);
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
    }
}
