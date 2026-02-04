using CoreLibraries.IO;
using System.Collections.Generic;
using VaultLib.Core.DataInterfaces;
using VaultLib.Core.Exports;

#nullable enable
namespace VaultLib.Core;

public class Vault<TKey> where TKey : struct, IKey<TKey>
{
    public Vault(VaultLib.Core.DB.Database<TKey> database, string name)
    {
        this.Database = database;
        this.Name = name;
        this.Exports = new List<BaseExport<TKey>>();
    }

    public string Name { get; }

    public ulong Version { get; set; }

    public List<BaseExport<TKey>> Exports { get; }

    public VaultLib.Core.DB.Database<TKey> Database { get; }

    public bool IsPrimaryVault { get; set; }

    public ByteOrder ByteOrder { get; set; }
}