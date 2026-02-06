using System;
using System.Collections;
using System.Reflection;
using AttribulatorUI;

namespace Attribulator.UI.PropertyGrid
{
    public class PropertyBoolItem : BaseBoolItem, ICommandName
    {
        private VLTBaseType prop;
        private PropertyInfo propertyInfo;

        public PropertyBoolItem(IParent parent, PropertyInfo propertyInfo, VLTBaseType prop, int padding) : base(parent, propertyInfo.Name, padding)
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
        private VLTBaseType prop;
        private PropertyInfo propertyInfo;

        public PropertyEnumItem(IParent parent, PropertyInfo propertyInfo, VLTBaseType prop, int padding) : base(parent, propertyInfo.Name, padding)
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

        public override void SetValue(object value)
        {
            this.propertyInfo.SetValue(this.prop, value);
        }
    }

    public class PropertyItem : BaseEditItem, ICommandName
    {
        private VLTBaseType prop;
        private PropertyInfo propertyInfo;

        public PropertyItem(IParent parent, PropertyInfo propertyInfo, VLTBaseType prop, int padding) : base(parent, propertyInfo.Name, padding)
        {
            this.prop = prop;
            this.propertyInfo = propertyInfo;
        }

        public string GetName()
        {
            return this.propertyInfo.Name;
        }

        public override object GetValue()
        {
            return this.propertyInfo.GetValue(this.prop);
        }

        public override void SetValue(object value)
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

        public override object GetValue()
        {
            return this.array[this.index];
        }

        public override void SetValue(object value)
        {
            this.array[this.index] = value;
        }
    }

    public class PropertyArrayItem : ArrayCollapseItem, IItemAddRemove, ICommandName
    {
        private VLTBaseType prop;
        private PropertyInfo propertyInfo;
        private int padding;
        private IParent parent;
        private int maxCount;
        private IList array;

        public PropertyArrayItem(IParent parent, PropertyInfo propertyInfo, VLTBaseType prop, int maxCount, int padding) : base(prop, propertyInfo.Name, prop.ToString(), padding)
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
                if (type.IsSubclassOf(typeof(VLTBaseType)))
                {
                    this.AddChild(new ClassItem(this, $"[{i}]", array[i] as VLTBaseType, padding + 21));
                }
                else if (GridHelper.IsStructWithPublicMembers(type))
                {
                    var index = i;
                    this.AddChild(new StructValueItem(
                        this,
                        $"[{i}]",
                        () => this.array[index],
                        v => this.array[index] = v,
                        padding));
                }
                else
                {
                    this.AddChild(new PropertyArraySubItem(parent, array, i, $"[{i}]", padding + 21));
                }
            }
            this.SortChildren();
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
        private readonly object owner;
        private readonly PropertyInfo propertyInfo;

        public MatrixItem(IParent parent, object owner, PropertyInfo propertyInfo, int padding)
            : base(propertyInfo.GetValue(owner), "Data", propertyInfo.GetValue(owner).ToString(), padding)
        {
            this.parent = parent;
            this.owner = owner;
            this.propertyInfo = propertyInfo;

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    var index = 4 * i + j;
                    this.AddChild(new MatrixElementItem(this, () => GetMatrixValue(index), v => SetMatrixValue(index, v),
                        $"[{i + 1},{j + 1}]", padding + 21));
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

        private VaultLib.Core.Types.Attrib.Types.Matrix GetMatrix()
        {
            return (VaultLib.Core.Types.Attrib.Types.Matrix)propertyInfo.GetValue(owner);
        }

        private void SetMatrix(VaultLib.Core.Types.Attrib.Types.Matrix matrix)
        {
            propertyInfo.SetValue(owner, matrix);
        }

        private float GetMatrixValue(int index)
        {
            var matrix = GetMatrix();
            return index switch
            {
                0 => matrix.M11,
                1 => matrix.M12,
                2 => matrix.M13,
                3 => matrix.M14,
                4 => matrix.M21,
                5 => matrix.M22,
                6 => matrix.M23,
                7 => matrix.M24,
                8 => matrix.M31,
                9 => matrix.M32,
                10 => matrix.M33,
                11 => matrix.M34,
                12 => matrix.M41,
                13 => matrix.M42,
                14 => matrix.M43,
                15 => matrix.M44,
                _ => 0
            };
        }

        private void SetMatrixValue(int index, object value)
        {
            var matrix = GetMatrix();
            var converted = Convert.ToSingle(value);
            switch (index)
            {
                case 0: matrix.M11 = converted; break;
                case 1: matrix.M12 = converted; break;
                case 2: matrix.M13 = converted; break;
                case 3: matrix.M14 = converted; break;
                case 4: matrix.M21 = converted; break;
                case 5: matrix.M22 = converted; break;
                case 6: matrix.M23 = converted; break;
                case 7: matrix.M24 = converted; break;
                case 8: matrix.M31 = converted; break;
                case 9: matrix.M32 = converted; break;
                case 10: matrix.M33 = converted; break;
                case 11: matrix.M34 = converted; break;
                case 12: matrix.M41 = converted; break;
                case 13: matrix.M42 = converted; break;
                case 14: matrix.M43 = converted; break;
                case 15: matrix.M44 = converted; break;
            }

            SetMatrix(matrix);
        }
    }

    public class MatrixElementItem : BaseEditItem, ICommandName
    {
        private readonly Func<object> getValue;
        private readonly Action<object> setValue;

        public MatrixElementItem(IParent parent, Func<object> getValue, Action<object> setValue, string name,
            int padding) : base(parent, name, padding)
        {
            this.getValue = getValue;
            this.setValue = setValue;
        }

        public string GetName()
        {
            return this.name;
        }

        public override object GetValue()
        {
            return this.getValue();
        }

        public override void SetValue(object value)
        {
            this.setValue(value);
        }
    }
}
