using System;
using System.Collections.Generic;
using System.Text;

namespace Attribulator.UI.PropertyGrid
{
    public class PrimitiveItem : BaseEditItem
    {
        private VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase prop;

        public PrimitiveItem(IParent parent, string name, VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase prop, int padding) : base(parent, name, padding)
        {
            this.prop = prop;
        }

        public override IConvertible GetValue()
        {
            return this.prop.GetValue();
        }

        public override void SetValue(IConvertible value)
        {
            this.prop.SetValue(value);
        }
    }

    public class PrimitiveBoolItem : BaseBoolItem
    {
        private VaultLib.Core.Types.EA.Reflection.Bool prop;

        public PrimitiveBoolItem(IParent parent, string name, VaultLib.Core.Types.EA.Reflection.Bool prop, int padding) : base(parent, name, padding)
        {
            this.prop = prop;
        }

        public override bool GetValue()
        {
            return this.prop.Value;
        }

        public override void SetValue(bool val)
        {
            this.prop.Value = val;
        }
    }

    public class PrimitiveEnumItem : BaseEnumItem
    {
        private VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase prop;

        public PrimitiveEnumItem(IParent parent, string name, VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase prop, int padding) : base(parent, name, padding)
        {
            this.prop = prop;
        }

        public override Enum GetValue()
        {
            return this.prop.GetValue() as Enum;
        }

        public override void SetValue(Enum val)
        {
            this.prop.SetValue(val);
        }
    }
}
