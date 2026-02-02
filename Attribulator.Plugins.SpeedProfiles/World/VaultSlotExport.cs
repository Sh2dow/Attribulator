using System.IO;
using CoreLibraries.GameUtilities;
using VaultLib.Core;
using VaultLib.Core.Exports;
using VaultLib.Core.Hashing;

namespace Attribulator.Plugins.SpeedProfiles.World
{
public class VaultSlotExport : BaseExport<Key32>
    {
        public override void Read(VaultReadContext<Key32> context, BinaryReader br)
        {
            br.ReadUInt32();
        }

        public override void Write(VaultWriteContext<Key32> context, BinaryWriter bw)
        {
            bw.Write(0);
        }

        public override Key32 GetExportId()
        {
            return new Key32(Vlt32Hasher.Hash("VaultData"));
        }

        public override string GetTypeId()
        {
            return "VaultDataType";
        }
    }
}
