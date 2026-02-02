using System.Collections.Generic;
using System.Linq;
using Attribulator.ModScript.API;
using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.Hashing;
using VaultLib.Core.Types;

namespace Attribulator.Plugins.ModScript.Commands
{
    // add_node class parentNode nodeName
    public class AddNodeModScriptCommand : BaseModScriptCommand
    {
        public string ClassName { get; set; }
        public string ParentCollectionName { get; set; }
        public string CollectionName { get; set; }

        public override void Parse(List<string> parts)
        {
            if (parts.Count != 3 && parts.Count != 4)
                throw new CommandParseException($"3 or 4 tokens expected, got {parts.Count}");

            ClassName = CleanHashString(parts[1]);
            ParentCollectionName = parts.Count == 4 ? CleanHashString(parts[2]) : "";
            CollectionName = CleanHashString(parts[^1]);
        }

        public override void Execute(DatabaseHelper databaseHelper)
        {
            VltCollection parentCollection = null;
            if (!string.IsNullOrEmpty(ParentCollectionName))
                if ((parentCollection = GetCollection(databaseHelper, ClassName, ParentCollectionName, false)) == null)
                    throw new CommandExecutionException(
                        $"add_node failed because parent collection does not exist: {ClassName}/{ParentCollectionName}");

            if (GetCollection(databaseHelper, ClassName, CollectionName, false) != null)
                throw new CommandExecutionException(
                    $"add_node failed because collection already exists: {ClassName}/{CollectionName}");

            Vault addToVault;

            if (parentCollection != null)
                addToVault = parentCollection.Vault;
            else
                addToVault = databaseHelper.Vaults.FirstOrDefault(vault =>
                    databaseHelper.GetCollectionsInVault(vault)
                        .Any(collection => ResolveName(collection.Class.Key) == ClassName));

            if (addToVault == null)
                throw new CommandExecutionException("failed to determine vault to insert new collection into");

            var newNode = databaseHelper.AddCollection(addToVault, ClassName, CollectionName, parentCollection);
            var vltClass = newNode.Class;

            //if (parentCollection != null)
            //    databaseHelper.CopyCollection(databaseHelper.Database, parentCollection, newNode);
            //else
                foreach (var baseField in vltClass.BaseFields)
                {
                    var registry = databaseHelper.Database.TypeRegistry;
                    var fieldType = registry.ResolveFieldType(baseField);
                    object vltBaseType = registry.ConstructTypeInstance(fieldType);

                    if (baseField.IsArray && vltBaseType is VLTArrayType array)
                    {
                        array.Capacity = baseField.MaxCount;
                        var itemType = array.ItemType;
                        for (var i = 0; i < array.Capacity; i++)
                            array.Items.Add(registry.ConstructTypeInstance(itemType));
                    }

                    newNode.SetRawValue(baseField.Key, vltBaseType);
                }

            if (vltClass.HasField("CollectionName")) newNode.SetRawValue("CollectionName", CollectionName);
            HashManager.AddVlt(CollectionName);
        }
    }
}
