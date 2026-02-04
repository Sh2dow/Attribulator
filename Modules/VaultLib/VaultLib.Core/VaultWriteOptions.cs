namespace VaultLib.Core;

public class VaultWriteOptions
{
    public VaultHashMode HashMode { get; init; } = VaultHashMode.Hash32;

    public VaultWriteQuirks Quirks { get; init; } = new VaultWriteQuirks();
}