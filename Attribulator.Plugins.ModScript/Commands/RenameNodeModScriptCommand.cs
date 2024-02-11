using System.Collections.Generic;
using Attribulator.ModScript.API;
using VaultLib.Core.Hashing;

namespace Attribulator.Plugins.ModScript.Commands
{
    // rename_node class node name
    public class RenameNodeModScriptCommand : BaseModScriptCommand
    {
        public string ClassName { get; set; }
        public string CollectionName { get; set; }
        public string NewName { get; set; }

        public override void Parse(List<string> parts)
        {
            if (parts.Count != 4) throw new CommandParseException($"Expected 4 tokens, got {parts.Count}");

            ClassName = CleanHashString(parts[1]);
            CollectionName = CleanHashString(parts[2]);
            NewName = CleanHashString(parts[3]);
        }

        public override void Execute(DatabaseHelper databaseHelper)
        {
            if (GetCollection(databaseHelper, ClassName, NewName, false) != null)
                throw new CommandExecutionException(
                    $"rename_node failed because there is already a collection called '{NewName}'");

            var collection = GetCollection(databaseHelper, ClassName, CollectionName);

            RemoveCollectionFromCache(collection);
            databaseHelper.RenameCollection(collection, NewName);
            HashManager.AddVLT(NewName);
        }
    }
}