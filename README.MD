• Comparing NFS-VltEd’s writer/reader to VaultLib (Attribulator/OGVI). The incompatibility is not the pack header; it’s the vault stream format and quirks per game version.

Key compatibility requirements from NFS-VltEd code

1. Version chunk (Vers) is only present for Carbon+
  - VltEd only reads/writes Vers for GEnum1.const_4..const_8 (Carbon, ProStreet, Undercover, World, World Alpha).
  - For Most Wanted (const_3) there is no Vers chunk.
  - VaultLib always writes VltVersionChunk in VaultWriter.BuildVltStream().
2. End chunk (EndC) exists only for World
  - VltEd writes/reads EndC only for GEnum1.const_7 (World).
  - VaultLib writes EndChunk whenever VaultWriteQuirks.EnableVltEndChunk is true (default).
3. Dependency vs Start chunk order depends on game
  - VltEd order is:
    - MW / Carbon / ProStreet (const_3..const_5): DepN then StrN
    - Undercover / World / World Alpha (const_6..const_8): StrN then DepN
  - VaultLib uses VaultWriteQuirks.StartChunkBeforeDepChunk (default false).
4. Layout size consistency (RequiredFieldsSize / LayoutSize)
  - VltEd pads base field data to RequiredFieldsSize for each class.
  - If VaultLib writes more base‑field bytes than the class LayoutSize, VltEd will choke.
  - This is the same root cause as your read too much layout data error.

———

Concrete changes to make Attribulator/OGVI output VltEd‑compatible

A. Apply per-game writer quirks
Implement these in the save path (e.g. PackSavingOptions.VaultWriteOptions.Quirks for each profile).

- Most Wanted
  - WriteVersionChunk = false (needs a new quirk)
  - EnableVltEndChunk = false
  - StartChunkBeforeDepChunk = false (DepN before StrN)
- Carbon / ProStreet
  - WriteVersionChunk = true
  - EnableVltEndChunk = false
  - StartChunkBeforeDepChunk = false
- Undercover / World / World Alpha
  - WriteVersionChunk = true
  - EnableVltEndChunk = (World only)
  - StartChunkBeforeDepChunk = true (StrN before DepN)

Files to adjust:

- Modules/VaultLib/VaultLib.Core/VaultWriter.cs
  Add a WriteVersionChunk quirk and gate VltVersionChunk.
- Modules/VaultLib/VaultLib.Core/VaultWriteQuirks.cs
  Add WriteVersionChunk.
- Save code per profile (e.g. Attribulator.Plugins.SpeedProfiles\*.cs)
  Pass appropriate PackSavingOptions with quirks.

B. Fix layout size
Make sure VltClass.LayoutSize matches the max offset+size of base fields before writing:

- Update class layout size before serializing class definitions and base‑field layouts.
- Consider arrays: for base fields, layout size should be at least Offset + Size * MaxCount.

Files to adjust:

- Modules/VaultLib/VaultLib.LegacyBase/Exports/ClassLoad32.cs (writing)
- Modules/VaultLib/VaultLib.Core/Exports/... for other formats if used
- Possibly a helper in VltClass<TKey> or writer code to compute layout size.

———

Why this matches VltEd exactly
From NFS-VltEd:

- Chunk order: Vers?, DepN/StrN (order depends on version), DatN, ExpN, PtrN, EndC?
  - See ns0/GClass480.cs
- EndC only for World: if (genum1_0 == GEnum1.const_7) new GClass501().imethod_1(...)
- Vers only for Carbon+ in GClass480
- Layout padding uses RequiredFieldsSize in GClass537.method_2

———

Changes in Attribulator/OGVI:

1. Add WriteVersionChunk quirk.
2. Apply per‑game quirks in the SpeedProfiles save path.
3. Recompute layout size before writing.