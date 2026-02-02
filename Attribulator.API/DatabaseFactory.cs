using System;
using CoreLibraries.IO;
using VaultLib.Core;
using VaultLib.Core.DataInterfaces;
using VaultLib.Support.Carbon;
using VaultLib.Support.MostWanted;
using VaultLib.Support.ProStreet;
using VaultLib.Support.Undercover;
using VaultLib.Support.World;

namespace Attribulator.API
{
    public static class DatabaseFactory
    {
        public static Database Create(DatabaseOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (options.Type != DatabaseType.X86Database)
            {
                throw new NotSupportedException("Only X86Database (Key32) is supported in this build.");
            }

            BaseGameModule<Key32> module = options.GameId switch
            {
                "MOST_WANTED" => new VaultLib.Support.MostWanted.ModuleDef32(),
                "CARBON" => new VaultLib.Support.Carbon.ModuleDef32(),
                "PROSTREET" => new VaultLib.Support.ProStreet.ModuleDef(),
                "UNDERCOVER" => new VaultLib.Support.Undercover.ModuleDef(),
                "WORLD" => new VaultLib.Support.World.ModuleDef(),
                _ => throw new NotSupportedException($"Unknown game id '{options.GameId}'.")
            };

            var typeRegistryBuilder = new TypeRegistryBuilder<Key32>(ByteOrder.Little);
            module.RegisterTypes(typeRegistryBuilder);
            var exportFactory = module.CreateExportFactory();
            return new Database(exportFactory, typeRegistryBuilder);
        }
    }
}
