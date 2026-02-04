// Decompiled with JetBrains decompiler
// Type: VaultLib.Core.RowManager`1
// Assembly: VaultLib.Core, Version=3.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 5C6F7845-B34F-475F-8FB8-674F10DDCBBA
// Assembly location: D:\Repos\Games\NFSTools\Attribulator3-alpha4-20250204\VaultLib.Core.dll

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using VaultLib.Core.Data;
using VaultLib.Core.DataInterfaces;
using VaultLib.Core.DB;

#nullable enable
namespace VaultLib.Core;

public class RowManager<TKey> where TKey : struct, IKey<TKey>
{
    private readonly Database<TKey> _database;

    internal List<VltCollection<TKey>> Rows { get; }

    public RowManager(Database<TKey> database)
    {
        this._database = database;
        this.Rows = new List<VltCollection<TKey>>();
    }

    public IEnumerable<VltCollection<TKey>> GetCollectionsInVault(Vault<TKey> vault)
    {
        return this.Rows.Where<VltCollection<TKey>>((Func<VltCollection<TKey>, bool>)(c => c.Vault == vault));
    }

    public IReadOnlyList<VltCollection<TKey>> GetCollections()
    {
        return (IReadOnlyList<VltCollection<TKey>>)this.Rows;
    }

    public List<VltCollection<TKey>> GetCollections(TKey classKey)
    {
        return this.EnumerateCollections(classKey).ToList<VltCollection<TKey>>();
    }

    public List<VltCollection<TKey>> GetCollections(string className)
    {
        return this.GetCollections(TKey.FromString(className));
    }

    public IEnumerable<VltCollection<TKey>> EnumerateCollections()
    {
        return (IEnumerable<VltCollection<TKey>>)this.Rows;
    }

    public IEnumerable<VltCollection<TKey>> EnumerateCollections(TKey classKey)
    {
        return this.Rows.Where<VltCollection<TKey>>((Func<VltCollection<TKey>, bool>)(c => c.Class.Key == classKey));
    }

    public IEnumerable<VltCollection<TKey>> EnumerateCollections(string className)
    {
        return this.EnumerateCollections(TKey.FromString(className));
    }

    public VltCollection<TKey>? FindCollection(TKey classKey, TKey collectionKey)
    {
        return this.EnumerateCollections(classKey)
            .FirstOrDefault<VltCollection<TKey>>(
                (Func<VltCollection<TKey>, bool>)(collection => collection.Key == collectionKey));
    }

    public VltCollection<TKey>? FindCollection(string className, string collectionName)
    {
        return this.FindCollection(TKey.FromString(className), TKey.FromString(collectionName));
    }

    public VltCollection<TKey> AddCollection(
        Vault<TKey> vault,
        TKey classKey,
        TKey key,
        VltCollection<TKey>? parentCollection = null)
    {
        if (this.FindCollection(classKey, key) != null)
            throw new DuplicateNameException("The specified key is already in use by another collection");
        VltCollection<TKey> collection = new VltCollection<TKey>(vault, this._database.FindClass(classKey), key);
        parentCollection?.AddChild(collection);
        this.Rows.Add(collection);
        return collection;
    }

    public VltCollection<TKey> AddCollection(
        Vault<TKey> vault,
        string className,
        string name,
        VltCollection<TKey>? parentCollection = null)
    {
        return this.AddCollection(vault, TKey.FromString(className), TKey.FromString(name), parentCollection);
    }

    public void AddCollection(VltCollection<TKey> collection) => this.Rows.Add(collection);

    public void RemoveCollection(VltCollection<TKey> collection) => this.Rows.Remove(collection);
}