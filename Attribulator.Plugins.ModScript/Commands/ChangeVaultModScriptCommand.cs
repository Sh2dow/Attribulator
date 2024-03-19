using System.Collections.Generic;
using System.Linq;
using Attribulator.API.Data;
using Attribulator.ModScript.API;
using VaultLib.Core.Hashing;

namespace Attribulator.Plugins.ModScript.Commands
{
    // change_vault class node vaultName
    public class ChangeVaultModScriptCommand : BaseModScriptCommand
    {
        public string ClassName { get; set; }
        public string CollectionName { get; set; }
        public string VaultName { get; set; }

        public override void Parse(List<string> parts)
        {
            if (parts.Count != 4)
                throw new CommandParseException($"Expected 4 tokens, got {parts.Count} ({string.Join(' ', parts)})");

            ClassName = CleanHashString(parts[1]);
            CollectionName = CleanHashString(parts[2]);
            VaultName = CleanHashString(parts[3]);
        }

        public override void Execute(DatabaseHelper databaseHelper)
        {
            var collection = GetCollection(databaseHelper, ClassName, CollectionName);
            var vault = databaseHelper.Database.Vaults.Find(v => v.Name == VaultName);
            LoadedFile gameplayFile = databaseHelper.Files.First(x => x.Name == "gameplay");

            if (vault == null)
            {
                if (collection.Class.Name == "gameplay")
                {
                    vault = new VaultLib.Core.Vault(VaultName);
                    vault.Database = databaseHelper.Database;

                    gameplayFile.Vaults.Add(vault);
                    databaseHelper.Database.Vaults.Add(vault);
                }
                else
                {
                    throw new CommandExecutionException($"Cannot find vault: {VaultName}");
                }
            }

            var oldVault = collection.Vault;
            collection.SetVault(vault);
            var oldVaultCollections = databaseHelper.GetCollectionsInVault(oldVault);
            if (!oldVaultCollections.Any())
            {
                gameplayFile.Vaults.Remove(oldVault);
                databaseHelper.Database.Vaults.Remove(oldVault);
            }

            HashManager.AddUserHash(VaultName);
        }
    }
}