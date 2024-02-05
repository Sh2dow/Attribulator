using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Attribulator.UI.PropertyGrid
{
    public class PropertyBoolItem : BaseBoolItem
    {
        private VaultLib.Core.Types.VLTBaseType prop;
        private PropertyInfo propertyInfo;

        public PropertyBoolItem(IParent parent, PropertyInfo propertyInfo, VaultLib.Core.Types.VLTBaseType prop, int padding) : base(parent, propertyInfo.Name, padding)
        {
            this.prop = prop;
            this.propertyInfo = propertyInfo;
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

    public class PropertyEnumItem : BaseEnumItem
    {
        private VaultLib.Core.Types.VLTBaseType prop;
        private PropertyInfo propertyInfo;

        public PropertyEnumItem(IParent parent, PropertyInfo propertyInfo, VaultLib.Core.Types.VLTBaseType prop, int padding) : base(parent, propertyInfo.Name, padding)
        {
            this.prop = prop;
            this.propertyInfo = propertyInfo;
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

    public class PropertyItem : BaseEditItem
    {
        private VaultLib.Core.Types.VLTBaseType prop;
        private PropertyInfo propertyInfo;

        public PropertyItem(IParent parent, PropertyInfo propertyInfo, VaultLib.Core.Types.VLTBaseType prop, int padding) : base(parent, propertyInfo.Name, padding)
        {
            this.prop = prop;
            this.propertyInfo = propertyInfo;
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
}
