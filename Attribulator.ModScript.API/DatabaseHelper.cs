using System.Collections.Generic;
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
            Collections = BuildCollectionIndex(database);
            Files = files;
        }

        public Dictionary<string, VltCollection> Collections { get; }
        public Database Database { get; }
        public List<Vault> Vaults => Database.Vaults;
        public IEnumerable<LoadedFile> Files;

        private static string ResolveName(Key32 key)
        {
            return HashManager.ResolveVlt(key.Hash) ?? key.ToString();
        }

        private static Key32 ParseKey(string name)
        {
            if (name.StartsWith("0x") && uint.TryParse(name.Substring(2),
                    System.Globalization.NumberStyles.AllowHexSpecifier,
                    System.Globalization.CultureInfo.InvariantCulture, out var hexVal))
            {
                return new Key32(hexVal);
            }

            return Key32.FromString(name);
        }

        private static string GetShortPath(Key32 classKey, Key32 collectionKey)
        {
            return $"{ResolveName(classKey)}/{ResolveName(collectionKey)}";
        }

        private static string GetShortPath(VltCollection collection)
        {
            return GetShortPath(collection.Class.Key, collection.Key);
        }

        private static Dictionary<string, VltCollection> BuildCollectionIndex(Database database)
        {
            var collections = database.RowManager.GetCollections()
                .OrderByDescending(c => c.Vault != null && c.Vault.IsPrimaryVault);
            var lookup = new Dictionary<string, VltCollection>();

            foreach (var collection in collections)
            {
                var shortPath = GetShortPath(collection);
                if (!lookup.ContainsKey(shortPath))
                {
                    lookup.Add(shortPath, collection);
                }
            }

            return lookup;
        }

        public VltCollection FindCollectionByName(string className, string collectionName)
        {
            var key = GetShortPath(ParseKey(className), ParseKey(collectionName));
            if (Collections.TryGetValue(key, out var collection))
            {
                return collection;
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

            var collection = new VltCollection(addToVault, Database.FindClass(className), ParseKey(collectionName));
            return AddCollection(collection, parentCollection);
        }

        public VltCollection AddCollection(VltCollection collection, VltCollection parentCollection = null)
        {
            if (parentCollection != null)
                collection.SetParent(parentCollection);
            else
                Database.RowManager.AddCollection(collection);

            Collections[GetShortPath(collection)] = collection;

            return collection;
        }

        public void RenameCollection(VltCollection collection, string newName)
        {
            Collections.Remove(GetShortPath(collection));
            collection.SetKey(ParseKey(newName));
            if (collection.Class.HasField("CollectionName")) collection.SetRawValue("CollectionName", newName);
            Collections.Add(GetShortPath(collection), collection);
        }

        public List<VltCollection> RemoveCollection(VltCollection collection)
        {
            var removed = new List<VltCollection> { collection };

            // Disassociate children
            var hasParent = collection.Parent != null;
            collection.SetParent(null);
            Collections.Remove(GetShortPath(collection));

            foreach (var collectionChild in Database.RowManager.GetCollections()
                .Where(c => ReferenceEquals(c.Parent, collection)).ToList())
            {
                removed.AddRange(RemoveCollection(collectionChild));
            }

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
