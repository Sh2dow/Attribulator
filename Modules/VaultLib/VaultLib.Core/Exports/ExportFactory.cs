using System;
using System.Collections.Generic;
using VaultLib.Core.Data;
using VaultLib.Core.DataInterfaces;
using VaultLib.Core.Structures;

namespace VaultLib.Core.Exports;

public class ExportFactory<TKey> where TKey : struct, IKey<TKey>
{
    private readonly Func<BaseDatabaseLoad<TKey>> _databaseLoadFactory;
    private readonly Func<BaseClassLoad<TKey>> _classLoadFactory;
    private readonly Func<BaseCollectionLoad<TKey>> _collectionLoadFactory;
    private readonly Func<IExportEntry<TKey>> _exportEntryFactory;
    private readonly Func<IPtrRef<TKey>> _ptrRefFactory;
    private static readonly TKey ClassLoadDataKey = TKey.FromString("Attrib::ClassLoadData");
    private static readonly TKey CollectionLoadDataKey = TKey.FromString("Attrib::CollectionLoadData");
    private static readonly TKey DatabaseLoadDataKey = TKey.FromString("Attrib::DatabaseLoadData");

    private Dictionary<TKey, Func<BaseExport<TKey>>> ExportBuilders { get; } =
        new Dictionary<TKey, Func<BaseExport<TKey>>>();

    public ExportFactory(
        Func<BaseDatabaseLoad<TKey>> databaseLoadFactory,
        Func<BaseClassLoad<TKey>> classLoadFactory,
        Func<BaseCollectionLoad<TKey>> collectionLoadFactory,
        Func<IExportEntry<TKey>> exportEntryFactory,
        Func<IPtrRef<TKey>>? ptrRefFactory = null)
    {
        this._databaseLoadFactory = databaseLoadFactory;
        this._classLoadFactory = classLoadFactory;
        this._collectionLoadFactory = collectionLoadFactory;
        this._exportEntryFactory = exportEntryFactory;
        this._ptrRefFactory = ptrRefFactory ?? (Func<IPtrRef<TKey>>)(() => (IPtrRef<TKey>)new AttribPtrRef<TKey>());
        this.ExportBuilders.Add(ExportFactory<TKey>.ClassLoadDataKey, (Func<BaseExport<TKey>>)classLoadFactory);
        this.ExportBuilders.Add(ExportFactory<TKey>.CollectionLoadDataKey,
            (Func<BaseExport<TKey>>)collectionLoadFactory);
        this.ExportBuilders.Add(ExportFactory<TKey>.DatabaseLoadDataKey, (Func<BaseExport<TKey>>)databaseLoadFactory);
    }

    public void RegisterExportType<TExport>(TKey exportType) where TExport : BaseExport<TKey>, new()
    {
        this.ExportBuilders.Add(exportType, (Func<BaseExport<TKey>>)(() => (BaseExport<TKey>)new TExport()));
    }

    public BaseExport<TKey> CreateExport(TKey exportType)
    {
        Func<BaseExport<TKey>> func;
        if (!this.ExportBuilders.TryGetValue(exportType, out func))
            throw new KeyNotFoundException($"No factory found for export type: {exportType}");
        return func();
    }

    public BaseCollectionLoad<TKey> BuildCollectionLoad(VltCollection<TKey> collection)
    {
        BaseCollectionLoad<TKey> baseCollectionLoad = this._collectionLoadFactory();
        baseCollectionLoad.Collection = collection;
        return baseCollectionLoad;
    }

    public BaseClassLoad<TKey> BuildClassLoad(VltClass<TKey> vltClass)
    {
        BaseClassLoad<TKey> baseClassLoad = this._classLoadFactory();
        baseClassLoad.Class = vltClass;
        return baseClassLoad;
    }

    public BaseDatabaseLoad<TKey> BuildDatabaseLoad() => this._databaseLoadFactory();

    public IPtrRef<TKey> CreatePtrRef() => this._ptrRefFactory();

    public IExportEntry<TKey> BuildExportEntry() => this._exportEntryFactory();
}