using CoreLibraries.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.Exports;
using VaultLib.Core.Types;
using VaultLib.Core.Types.EA.Reflection;
using VaultLib.Core.Utils;
using VaultLib.ModernBase.Exports;

namespace VaultLib.ModernBase
{
    public abstract class ModernCollectionLoadBase<TAttribEntry> : BaseCollectionLoad
        where TAttribEntry : AttribEntryBase
    {
        protected uint LayoutPointer { get; set; }

        protected uint[] Types { get; set; }

        protected List<TAttribEntry> Entries { get; set; }

        protected long SourceLayoutPointer { get; set; }

        private long DestinationLayoutPointer { get; set; }

        public override void ReadPointerData(Vault vault, BinaryReader br)
        {
            if (LayoutPointer != 0)
            {
                br.BaseStream.Position = LayoutPointer;

                foreach (var baseField in Collection.Class.BaseFields)
                {
                    br.AlignReader(baseField.Alignment);
                    long currentOffset = br.BaseStream.Position - LayoutPointer;
                    if (currentOffset != baseField.Offset)
                    {
                        Debug.WriteLine(
                            $"[Warning] Field {baseField.Name} offset mismatch: current offset = 0x{currentOffset:X}, expected = 0x{baseField.Offset:X}. Continuing with current offset.");
                        // Instead of throwing an exception, we simply continue reading from the current offset.
                    }

                    VLTBaseType data = TypeRegistry.CreateInstance(vault.Database.Options.GameId, Collection.Class,
                        baseField, Collection);
                    long startPos = br.BaseStream.Position;
                    data.Read(vault, br);
                    long endPos = br.BaseStream.Position;

                    if (data is PrimitiveTypeBase)
                        br.BaseStream.Position = startPos + baseField.Size;

                    if (!(data is VLTArrayType) && !(data is PrimitiveTypeBase))
                    {
                        if (endPos - startPos != baseField.Size)
                        {
                            Debug.WriteLine(
                                $"[Warning] Field {baseField.Name} read {endPos - startPos} bytes (expected {baseField.Size}).");
                        }
                    }

                    Collection.SetRawValue(baseField.Name, data);
                }
            }

            // Process entries.
            foreach (var entry in Entries)
            {
                var optionalField = Collection.Class[entry.Key];

                if ((optionalField.Flags & DefinitionFlags.IsStatic) != 0)
                    throw new Exception("Encountered static field as an entry. Processing will not continue.");

                if ((optionalField.Flags & DefinitionFlags.HasHandler) != 0)
                {
                    Debug.Assert((entry.NodeFlags & NodeFlagsEnum.HasHandler) == NodeFlagsEnum.HasHandler);
                }
                else
                {
                    Debug.Assert((entry.NodeFlags & NodeFlagsEnum.HasHandler) == 0);
                }

                if ((optionalField.Flags & DefinitionFlags.Array) != 0)
                {
                    Debug.Assert((entry.NodeFlags & NodeFlagsEnum.IsArray) == NodeFlagsEnum.IsArray);
                }
                else
                {
                    Debug.Assert((entry.NodeFlags & NodeFlagsEnum.IsArray) == 0);
                }

                if (entry.InlineData is VLTAttribType attribType)
                {
                    Debug.Assert((entry.NodeFlags & NodeFlagsEnum.IsInline) == 0);
                    attribType.ReadPointerData(vault, br);
                    Collection.SetRawValue(optionalField.Name, attribType.Data);
                }
                else
                {
                    Debug.Assert((entry.NodeFlags & NodeFlagsEnum.IsInline) == NodeFlagsEnum.IsInline);
                    Collection.SetRawValue(optionalField.Name, entry.InlineData);
                }
            }

            foreach (var dataEntry in Collection.GetData())
            {
                if (dataEntry.Value is IPointerObject pointerObject)
                    pointerObject.ReadPointerData(vault, br);
            }
        }

        public override void WritePointerData(Vault vault, BinaryWriter bw)
        {
            foreach (var baseField in Collection.Class.BaseFields)
            {
                bw.AlignWriter(baseField.Alignment);
                if (DestinationLayoutPointer == 0)
                {
                    DestinationLayoutPointer = bw.BaseStream.Position;
                }

                long currentRelative = bw.BaseStream.Position - DestinationLayoutPointer;
                if (currentRelative != baseField.Offset)
                {
                    long diff = baseField.Offset - currentRelative;
                    if (diff > 0)
                    {
                        // Pad with zeros until we reach the expected offset.
                        bw.Write(new byte[diff]);
                        Debug.WriteLine($"[Warning] Padding {diff} bytes before writing field {baseField.Name}.");
                    }
                    else
                    {
                        Debug.WriteLine(
                            $"[Warning] Current position is {currentRelative} (expected {baseField.Offset}) for field {baseField.Name}. Continuing without padding.");
                        // Optionally, you might choose to throw an exception here if being behind is unacceptable.
                    }
                }

                // Now write the field's pointer data.
                Collection.GetRawValue(baseField.Name).Write(vault, bw);
            }

            foreach (var dataPair in Collection.GetData())
            {
                VltClassField field = Collection.Class[dataPair.Key];

                if (!field.IsInLayout)
                {
                    var entry = Entries.First(e => e.Key == field.Key);
                    if (!(entry.InlineData is IPointerObject pointerObject)) continue;
                    bw.AlignWriter(field.Alignment);
                    pointerObject.WritePointerData(vault, bw);
                }
                else
                {
                    if (!(dataPair.Value is IPointerObject pointerObject)) continue;
                    bw.AlignWriter(field.Alignment);
                    pointerObject.WritePointerData(vault, bw);
                }
            }

            if (Collection.Class.HasBaseFields)
            {
                // align to 4 bytes for layout data
                bw.AlignWriter(4);
            }
            else
            {
                // there is no layout data but we might still be in a bad position, so align to 2 bytes
                bw.AlignWriter(2);
            }
        }


        public override void AddPointers(Vault vault)
        {
            vault.SaveContext.AddPointer(SourceLayoutPointer, DestinationLayoutPointer, true);

            foreach (var baseField in Collection.Class.BaseFields)
            {
                if (this.Collection.GetRawValue(baseField.Name) is IPointerObject pointerObject)
                {
                    pointerObject.AddPointers(vault);
                }
            }

            foreach (var entry in Entries)
            {
                if (entry.InlineData is IPointerObject pointerObject)
                {
                    pointerObject.AddPointers(vault);
                }
            }
        }
    }
}