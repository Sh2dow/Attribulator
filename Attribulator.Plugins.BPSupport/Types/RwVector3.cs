using System.IO;
using VaultLib.Core;
using VaultLib.Core.Types;

namespace Attribulator.Plugins.BPSupport.Types
{
    public class RwVector3 : VLTBaseType
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public override void Read(VaultReadContext<Key32> context, FieldReadWriteContext<Key32> fieldContext,
            BinaryReader br)
        {
            X = br.ReadSingle();
            Y = br.ReadSingle();
            Z = br.ReadSingle();
            br.ReadUInt32();
        }

        public override void Write(VaultWriteContext<Key32> context, FieldReadWriteContext<Key32> fieldContext,
            BinaryWriter bw)
        {
            bw.Write(X);
            bw.Write(Y);
            bw.Write(Z);
            bw.Write(0);
        }

        public override object Clone()
        {
            return new RwVector3 { X = X, Y = Y, Z = Z };
        }
    }
}
