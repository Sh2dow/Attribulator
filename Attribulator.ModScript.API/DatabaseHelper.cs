﻿using System.Collections.Generic;
using System.Data;
using System.Linq;
using Attribulator.API.Data;
using Attribulator.ModScript.API.Utils;
using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.DB;
using VaultLib.Core.Hashing;

namespace Attribulator.ModScript.API
{
    public class DatabaseHelper
    {
        public DatabaseHelper(Database database, IEnumerable<LoadedFile> files = null)
        {
            Database = database;
            Collections = database.RowManager.GetFlattenedCollections().ToDictionary(c => c.ShortPath, c => c);
            Files = files;
        }

        public Dictionary<string, VltCollection> Collections { get; }
        public Database Database { get; }
        public List<Vault> Vaults => Database.Vaults;
        public IEnumerable<LoadedFile> Files;

        public VltCollection FindCollectionByName(string className, string collectionName)
        {
            var key = $"{className}/{collectionName}";
            if (Collections.TryGetValue(key, out var collection))
            {
                return collection;
            }
            else
            {
                key = $"{className}/0x{VLT32Hasher.Hash(collectionName).ToString("X8")}";
                if (Collections.TryGetValue(key, out collection))
                {
                    return collection;
                }
            }

            return null;
        }

        public IEnumerable<VltCollection> GetCollectionsInVault(Vault vault)
        {
            return Collections.Values.Where(c => ReferenceEquals(c.Vault, vault));
        }

        public VltCollection AddCollection(Vault addToVault, string className, string collectionName,
            VltCollection parentCollection)
        {
            if (FindCollectionByName(className, collectionName) != null)
                throw new DuplicateNameException(
                    $"A collection in the class '{className}' with the name '{collectionName}' already exists.");

            var collection = new VltCollection(addToVault, Database.FindClass(className), collectionName);
            return AddCollection(collection, parentCollection);
        }

        public VltCollection AddCollection(VltCollection collection, VltCollection parentCollection = null)
        {
            if (parentCollection != null)
                parentCollection.AddChild(collection);
            else
                Database.RowManager.Rows.Add(collection);

            Collections[collection.ShortPath] = collection;

            return collection;
        }

        public void RenameCollection(VltCollection collection, string newName)
        {
            Collections.Remove(collection.ShortPath);
            collection.SetName(newName);
            if (collection.Class.HasField("CollectionName")) collection.SetDataValue("CollectionName", newName);
            Collections.Add(collection.ShortPath, collection);
        }

        public List<VltCollection> RemoveCollection(VltCollection collection)
        {
            var removed = new List<VltCollection> { collection };

            // Disassociate children
            var hasParent = collection.Parent != null;
            collection.Parent?.RemoveChild(collection);
            Collections.Remove(collection.ShortPath);

            foreach (var collectionChild in collection.Children.ToList())
                removed.AddRange(RemoveCollection(collectionChild));

            if (!hasParent) Database.RowManager.RemoveCollection(collection);

            return removed;
        }

        public void CopyCollection(Database database, VltCollection from, VltCollection to)
        {
            foreach (var dataPair in from.GetData())
            {
                var field = from.Class[dataPair.Key];
                to.SetRawValue(dataPair.Key,
                    ValueCloningUtils.CloneValue(database, dataPair.Value, to.Class, field, to));
            }
        }
    }
}