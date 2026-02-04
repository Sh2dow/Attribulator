using CoreLibraries.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using VaultLib.Core.Chunks;
using VaultLib.Core.Data;
using VaultLib.Core.DataInterfaces;
using VaultLib.Core.Exports;
using VaultLib.Core.IO;
using VaultLib.Core.Utils;

namespace VaultLib.Core.DB;

public class Database<TKey> where TKey : struct, IKey<TKey>
{
    private Dictionary<VltCollection<TKey>, TKey> _parentKeyDictionary = new Dictionary<VltCollection<TKey>, TKey>();

    public Database(DatabaseOptions options, VaultLib.Core.Exports.ExportFactory<TKey> exportFactory)
    {
        this.Options = options;
        this.Classes = new List<VltClass<TKey>>();
        this.Types = new List<DatabaseTypeInfo>();
        this.Vaults = new List<Vault<TKey>>();
        this.RowManager = new VaultLib.Core.RowManager<TKey>(this);
        this.TypeRegistry = new VaultLib.Core.TypeRegistry<TKey>(this);
        this.ExportFactory = exportFactory;
    }

    public DatabaseOptions Options { get; }

    public VaultLib.Core.RowManager<TKey> RowManager { get; }

    public List<VltClass<TKey>> Classes { get; }

    public List<DatabaseTypeInfo> Types { get; }

    public VaultLib.Core.TypeRegistry<TKey> TypeRegistry { get; }

    public VaultLib.Core.Exports.ExportFactory<TKey> ExportFactory { get; }

    public List<Vault<TKey>> Vaults { get; }

    public void AddClass(VltClass<TKey> vltClass) => this.Classes.Add(vltClass);

    public VltClass<TKey> FindClass(TKey key)
    {
        return this.Classes.First<VltClass<TKey>>((Func<VltClass<TKey>, bool>)(c => c.Key == key));
    }

    public VltClass<TKey> FindClass(string name) => this.FindClass(TKey.FromString(name));

    public Vault<TKey> FindVault(string name)
    {
        return this.Vaults.First<Vault<TKey>>((Func<Vault<TKey>, bool>)(v => v.Name == name));
    }

    public Vault<TKey> LoadVault(VaultReadWrapper readWrapper)
    {
        Vault<TKey> vault = new Vault<TKey>(this, readWrapper.VaultName)
        {
            ByteOrder = readWrapper.ByteOrder
        };
        BinaryReader streamReader1 = Database<TKey>.CreateStreamReader(readWrapper.BinStream, readWrapper.ByteOrder);
        BinaryReader streamReader2 = Database<TKey>.CreateStreamReader(readWrapper.VltStream, readWrapper.ByteOrder);
        Debug.WriteLine("[IN] vault {0}: bin size 0x{1:X} vlt size 0x{2:X}", (object)vault.Name,
            (object)readWrapper.BinStream.Length, (object)readWrapper.VltStream.Length);
        ChunkReader<TKey> chunkReader1 = new ChunkReader<TKey>(streamReader1);
        ChunkReader<TKey> chunkReader2 = new ChunkReader<TKey>(streamReader2);
        VaultReadContext<TKey> context =
            new VaultReadContext<TKey>(vault, readWrapper.BinStream, readWrapper.VltStream);
        this.processBinChunks(context, chunkReader1);
        this.processVltChunks(context, chunkReader2);
        this.fixPointers(context, VltPointerType.Bin, readWrapper.BinStream);
        this.fixPointers(context, VltPointerType.Vlt, readWrapper.VltStream);
        this.ReadExports(context, streamReader2, streamReader1);
        this.Vaults.Add(vault);
        return vault;
    }

    private static BinaryReader CreateStreamReader(Stream stream, ByteOrder byteOrder)
    {
        return byteOrder == ByteOrder.Big ? (BinaryReader)new BigEndianBinaryReader(stream) : new BinaryReader(stream);
    }

    public void CompleteLoad()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        Dictionary<VltClass<TKey>, Dictionary<TKey, VltCollection<TKey>>> dictionary1 =
            new Dictionary<VltClass<TKey>, Dictionary<TKey, VltCollection<TKey>>>();
        foreach (VltCollection<TKey> row in this.RowManager.Rows)
        {
            Dictionary<TKey, VltCollection<TKey>> dictionary2;
            if (!dictionary1.TryGetValue(row.Class, out dictionary2))
            {
                dictionary2 = new Dictionary<TKey, VltCollection<TKey>>();
                dictionary1.Add(row.Class, dictionary2);
            }

            if (!dictionary2.TryAdd(row.Key, row))
                Debug.WriteLine("WARN: duplicate key detected in class {2}: {0} (0x{1:X})", (object)row.Key,
                    (object)row.Key, (object)row.Class.Key);
        }

        foreach (VltCollection<TKey> row in this.RowManager.Rows)
        {
            TKey key;
            if (this._parentKeyDictionary.TryGetValue(row, out key))
            {
                VltCollection<TKey> vltCollection;
                if (!dictionary1[row.Class].TryGetValue(key, out vltCollection))
                    throw new Exception($"could not find parent collection for {row.Key}: {key}");
                vltCollection.AddChild(row);
            }
        }

        stopwatch.Stop();
        this._parentKeyDictionary.Clear();
        this.FixupStaticData();
    }

    private void FixupStaticData()
    {
    }

    private void ReadExports(
        VaultReadContext<TKey> context,
        BinaryReader vltStreamReader,
        BinaryReader binStreamReader)
    {
        foreach (BaseExport<TKey> export in context.Vault.Exports)
        {
            vltStreamReader.BaseStream.Position = (long)export.Offset;
            export.Read(context, vltStreamReader);
            if (vltStreamReader.BaseStream.Position - (long)export.Offset != (long)export.Size)
                throw new Exception();
            if (export is IPointerObject<TKey> pointerObject)
                pointerObject.ReadPointerData(context, binStreamReader);
            if (export is BaseCollectionLoad<TKey> baseCollectionLoad && baseCollectionLoad.ParentKey != TKey.Zero)
                this._parentKeyDictionary[baseCollectionLoad.Collection] = baseCollectionLoad.ParentKey;
        }

        context.Vault.IsPrimaryVault = context.Vault.Exports.OfType<BaseClassLoad<TKey>>().Any<BaseClassLoad<TKey>>();
    }

    private void fixPointers(
        VaultReadContext<TKey> context,
        VltPointerType pointerType,
        Stream stream)
    {
        IEnumerable<VltPointer> vltPointers =
            context.Pointers.Where<VltPointer>((Func<VltPointer, bool>)(pointer => pointer.Type == pointerType));
        bool flag = context.Vault.ByteOrder == ByteOrder.Big;
        foreach (VltPointer vltPointer in vltPointers)
        {
            stream.Position = (long)vltPointer.FixUpOffset;
            byte[] bytes = BitConverter.GetBytes(vltPointer.Destination);
            if (flag)
                Array.Reverse<byte>(bytes);
            stream.Write(bytes, 0, 4);
        }
    }

    private void processBinChunks(VaultReadContext<TKey> context, ChunkReader<TKey> chunkReader)
    {
        chunkReader.NextChunk().Read(context, chunkReader.Reader);
    }

    private void processVltChunks(VaultReadContext<TKey> context, ChunkReader<TKey> chunkReader)
    {
        while (chunkReader.Reader.BaseStream.Position < chunkReader.Reader.BaseStream.Length)
        {
            ChunkBase<TKey> chunkBase = chunkReader.NextChunk();
            chunkBase.Read(context, chunkReader.Reader);
            chunkBase.GoToEnd(chunkReader.Reader.BaseStream);
        }
    }
}