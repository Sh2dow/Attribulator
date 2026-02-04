using System;
using System.Collections.Generic;
using VaultLib.Core.DataInterfaces;
using VaultLib.Core.Types;

#nullable enable
namespace VaultLib.Core.Data;

public class VltCollection<TKey> where TKey : struct, IKey<TKey>
{
    public VltClass<TKey> Class { get; }

    public VaultLib.Core.Vault<TKey> Vault { get; private set; }

    public TKey Key { get; private set; }

    public VltCollection<TKey>? Parent { get; private set; }

    private VltDataTable<TKey> Data { get; }

    public VltCollection(VaultLib.Core.Vault<TKey> vault, VltClass<TKey> vltClass, TKey key)
    {
        this.Vault = vault;
        this.Class = vltClass;
        this.Key = key;
        this.Data = new VltDataTable<TKey>();
    }

    public void SetParent(VltCollection<TKey>? parent)
    {
        if (parent != null)
        {
            if (!ReferenceEquals(parent.Class, Class))
            {
                throw new ArgumentException("New parent collection belongs to a different class.");
            }
        }
        
        Parent = parent;
    }
    
    public void AddChild(VltCollection<TKey> collection) => collection.Parent = this;

    public void RemoveChild(VltCollection<TKey> collection)
    {
        collection.Parent = collection.Parent == this
            ? (VltCollection<TKey>)null
            : throw new ArgumentException("Attempted to disassociate a non-related collection");
    }

    public void SetVault(VaultLib.Core.Vault<TKey> vault) => this.Vault = vault;

    public void SetKey(TKey newKey)
    {
        if (this.Key == newKey)
            return;
        if (this.Vault.Database.RowManager.FindCollection(this.Class.Key, newKey) != null)
            throw new ArgumentException($"A collection with the same key ({newKey}) already exists", nameof(newKey));
        this.Key = newKey;
    }

    public IReadOnlyDictionary<TKey, object> GetData() => this.Data.GetDictionary();

    public IReadOnlyList<VltDataTable<TKey>.Entry> GetOrderedData() => this.Data.GetEntries();

    public bool HasEntry(TKey key) => this.Data.HasValue(key);

    public bool HasEntry(string name) => this.HasEntry(TKey.FromString(name));

    public object GetRawValue(TKey key) => this.GetRawValue<object>(key);

    public object GetRawValue(string name) => this.GetRawValue<object>(TKey.FromString(name));

    public T GetRawValue<T>(TKey key)
    {
        T rawValue;
        if (!this.Data.TryGetValue<T>(key, out rawValue))
            throw new KeyNotFoundException($"Collection does not have a value for field {key}");
        return rawValue;
    }

    public T GetRawValue<T>(string name) => this.GetRawValue<T>(TKey.FromString(name));

    public T GetRawValue<T>(TKey key, int index)
    {
        if (!(this.GetRawValue(key, index) is T rawValue))
            throw new InvalidCastException($"Field {key} is not compatible with type {typeof(T)}");
        return rawValue;
    }

    public T GetRawValue<T>(string name, int index)
    {
        return this.GetRawValue<T>(TKey.FromString(name), index);
    }

    public object GetRawValue(TKey key, int index)
    {
        VltArrayType<TKey> rawValue = this.GetRawValue<VltArrayType<TKey>>(key);
        if (index < 0 || index >= rawValue.Items.Count)
            throw new ArgumentException($"Failed condition: 0 <= {index} < {rawValue.Items.Count}");
        return rawValue.Items[index];
    }

    public object GetRawValue(string name, int index)
    {
        return this.GetRawValue(TKey.FromString(name), index);
    }

    public void SetRawValue(TKey key, object data)
    {
        if (this.Class.HasField(key))
            this.Data.SetValue(key, data);
        else
            throw new KeyNotFoundException($"Field '{key}' not found in class");
    }

    public void SetRawValue(string key, object data) => this.SetRawValue(TKey.FromString(key), data);

    public void SetRawValue<T>(TKey key, int index, T data) where T : notnull
    {
        VltArrayType<TKey> rawValue = this.GetRawValue<VltArrayType<TKey>>(key);
        if (index < 0 || index >= rawValue.Items.Count)
            throw new ArgumentException($"Failed condition: 0 <= {index} < {rawValue.Items.Count}");
        if (data.GetType() != rawValue.ItemType)
            throw new ArgumentException($"Type mismatch: T={data.GetType()} A={rawValue.ItemType}");
        rawValue.Items[index] = (object)data;
    }

    public void SetRawValue<T>(string key, int index, T data) where T : notnull
    {
        this.SetRawValue<T>(TKey.FromString(key), index, data);
    }

    public void RemoveValue(TKey key)
    {
        if (this.Class.HasField(key))
        {
            if (this.Class[key].IsInLayout)
                throw new Exception($"Cannot remove in-layout field: {key}");
            if (this.HasEntry(key))
                this.Data.RemoveValue(key);
            else
                throw new KeyNotFoundException($"Collection does not have an entry for '{key}'");
        }
        else
            throw new KeyNotFoundException($"Class does not have field '{key}'");
    }

    public void RemoveValue(string name) => this.RemoveValue(TKey.FromString(name));
    
}