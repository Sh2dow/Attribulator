using System.IO;
using VaultLib.Core;
using VaultLib.Core.Types;

namespace Attribulator.Plugins.BPSupport.Types
{
    public class RwVector2 : VLTBaseType
    {
        public float X { get; set; }
        public float Y { get; set; }

        public override void Read(VaultReadContext<Key32> context, FieldReadWriteContext<Key32> fieldContext,
            BinaryReader br)
        {
            X = br.ReadSingle();
            Y = br.ReadSingle();
        }

        public override void Write(VaultWriteContext<Key32> context, FieldReadWriteContext<Key32> fieldContext,
            BinaryWriter bw)
        {
            bw.Write(X);
            bw.Write(Y);
        }

        public override object Clone()
        {
            return new RwVector2 { X = X, Y = Y };
        }
    }
}
