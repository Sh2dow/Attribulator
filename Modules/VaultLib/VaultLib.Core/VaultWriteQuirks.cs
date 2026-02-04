namespace VaultLib.Core;

public class VaultWriteQuirks
{
    public bool StartChunkBeforeDepChunk { get; set; }

    public bool EnableBinEndChunk { get; set; }

    public bool EnableVltEndChunk { get; set; } = true;
}