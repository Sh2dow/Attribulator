// This file is part of VaultLib by heyitsleo.
// 
// Created: 09/25/2019 @ 7:12 PM.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using VaultLib.Core.Data;
using VaultLib.Core.Utils;

namespace VaultLib.Core.Types
{
    public abstract class VLTBaseType : IFileAccess
    {
        protected VLTBaseType(VltClass @class, VltClassField field, VltCollection collection)
        {
            Class = @class;
            Field = field;
            Collection = collection;
        }

        protected VLTBaseType(VltClass @class, VltClassField field)
        {
            Class = @class;
            Field = field;
        }

        protected VltClass Class { get; set; }
        protected VltClassField Field { get; set; }
        protected VltCollection Collection { get; set; }

        protected bool IsInVLT
        {
            get { return !Field.IsInLayout && Field.Size <= 4 && !Field.IsArray; }
        }

        public abstract void Read(Vault vault, BinaryReader br);
        public abstract void Write(Vault vault, BinaryWriter bw);

        public override string ToString()
        {
            var properties = this.GetType().GetProperties();
            var strs = properties.Select(property =>
            {
                var val = property.GetValue(this);
                string value = val is IConvertible ? (val as IConvertible).ToString(CultureInfo.InvariantCulture) : (val?.ToString() ?? "");
                return $"{property.Name}: {value}";
            });
            return string.Join(" | ", strs);
        }
    }
}