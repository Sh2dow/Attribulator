using System;

namespace Attribulator.UI.PropertyGrid
{
    public class PrimitiveItem : BaseEditItem, ICommandName
    {
        private VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase prop;
        private string name;

        public PrimitiveItem(IParent parent, string name, VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase prop, Type type, int padding) : base(parent, name, type, padding)
        {
            this.prop = prop;
            this.name = name;
        }

        public string GetName()
        {
            return this.name;
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

    public class PrimitiveBoolItem : BaseBoolItem, ICommandName
    {
        private VaultLib.Core.Types.EA.Reflection.Bool prop;
        private string name;

        public PrimitiveBoolItem(IParent parent, string name, VaultLib.Core.Types.EA.Reflection.Bool prop, int padding) : base(parent, name, padding)
        {
            this.prop = prop;
            this.name = name;
        }

        public string GetName()
        {
            return this.name;
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

    public class PrimitiveEnumItem : BaseEnumItem, ICommandName
    {
        private VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase prop;
        private string name;

        public PrimitiveEnumItem(IParent parent, string name, VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase prop, int padding) : base(parent, name, padding)
        {
            this.prop = prop;
            this.name = name;
        }

        public string GetName()
        {
            return this.name;
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
