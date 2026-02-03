using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using System.IO;
using System.Linq;
using CoreLibraries.IO;
using VaultLib.Core;
using VaultLib.Core.Pack;
using VaultLib.Core.DataInterfaces;

namespace Attribulator.Plugins.BPSupport
{
    public class BurnoutVaultPack : IVaultPack
    {
        private readonly string _vaultName;

        public BurnoutVaultPack(string vaultName)
        {
            _vaultName = vaultName;
        }

        public IList<Vault<TKey>> Load<TKey>(BinaryReader br, VaultLib.Core.DB.Database<TKey> database,
            PackLoadingOptions? loadingOptions = null) where TKey : struct, IKey<TKey>
        {
            if (typeof(TKey) != typeof(Key32))
                throw new NotSupportedException("BurnoutVaultPack only supports Key32 databases.");

            var typedDatabase = (Database)(object)database;
            var loaded = LoadKey32(br, typedDatabase, loadingOptions);
            return loaded.Cast<Vault<TKey>>().ToList();
        }

        private IList<Vault> LoadKey32(BinaryReader br, Database database, PackLoadingOptions? loadingOptions)
        {
            var vltOffset = br.ReadUInt32();
            var vltSize = br.ReadUInt32();
            var binOffset = br.ReadUInt32();
            var binSize = br.ReadUInt32();

            if (vltOffset > br.BaseStream.Length)
                throw new InvalidDataException();

            if (binOffset > br.BaseStream.Length)
                throw new InvalidDataException();

            br.BaseStream.Position = vltOffset;
            var vltData = new byte[vltSize];

            if (br.Read(vltData) != vltData.Length) throw new InvalidDataException();

            br.BaseStream.Position = binOffset;
            var binData = new byte[binSize];

            if (br.Read(binData) != binData.Length) throw new InvalidDataException();

            using var readWrapper = new VaultReadWrapper(
                _vaultName,
                new MemoryStream(binData),
                new MemoryStream(vltData),
                loadingOptions?.ByteOrder ?? ByteOrder.Little);
            var vault = database.LoadVault(readWrapper);

            return new ReadOnlyCollection<Vault>(new List<Vault>(new[] {vault}));
        }

        public void Save<TKey>(BinaryWriter bw, IList<Vault<TKey>> vaults, PackSavingOptions? savingOptions = null)
            where TKey : struct, IKey<TKey>
        {
            if (typeof(TKey) != typeof(Key32))
                throw new NotSupportedException("BurnoutVaultPack only supports Key32 databases.");

            var typedVaults = vaults.Cast<Vault>().ToList();
            SaveKey32(bw, typedVaults, savingOptions);
        }

        private void SaveKey32(BinaryWriter bw, IList<Vault> vaults, PackSavingOptions? savingOptions)
        {
            bw.Write(0x10);
            var vault = vaults[0];
            var writeOptions = savingOptions?.VaultWriteOptions ?? new VaultWriteOptions();
            var vw = new VaultWriter<Key32>(vault, writeOptions);
            var streamInfo = vw.BuildVault();
            bw.Write((uint) streamInfo.VltStream.Length);
            bw.Write(0);
            bw.Write((uint) streamInfo.BinStream.Length);

            streamInfo.VltStream.CopyTo(bw.BaseStream);
            bw.AlignWriter(0x10);
            var binOffset = bw.BaseStream.Position;
            streamInfo.BinStream.CopyTo(bw.BaseStream);
            var endOffset = bw.BaseStream.Position;

            bw.BaseStream.Position = 8;
            bw.Write((uint) binOffset);
            bw.BaseStream.Position = endOffset;
        }
    }
}
