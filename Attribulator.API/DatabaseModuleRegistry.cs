using System;
using System.Collections.Generic;
using VaultLib.Core;
using VaultLib.Core.DataInterfaces;

namespace Attribulator.API
{
    public static class DatabaseModuleRegistry
    {
        private static readonly Dictionary<string, BaseGameModule<Key32>> Modules =
            new(StringComparer.OrdinalIgnoreCase);

        public static void Register(string gameId, BaseGameModule<Key32> module)
        {
            if (string.IsNullOrWhiteSpace(gameId))
                throw new ArgumentException("Game ID cannot be null or empty.", nameof(gameId));
            if (module == null) throw new ArgumentNullException(nameof(module));

            Modules[gameId] = module;
        }

        public static bool TryGet(string gameId, out BaseGameModule<Key32> module)
        {
            return Modules.TryGetValue(gameId, out module);
        }
    }
}
