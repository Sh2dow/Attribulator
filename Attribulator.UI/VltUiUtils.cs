using System.Collections.Generic;
using System.Linq;
using VaultLib.Core.Hashing;

namespace Attribulator.UI
{
    public static class VltUiUtils
    {
        public static string ResolveName(Key32 key)
        {
            return HashManager.ResolveVlt(key.Hash) ?? key.ToString();
        }

        public static string GetName(VltClass vltClass)
        {
            return ResolveName(vltClass.Key);
        }

        public static string GetName(VltCollection vltCollection)
        {
            return ResolveName(vltCollection.Key);
        }

        public static string GetShortPath(VltCollection vltCollection)
        {
            return $"{GetName(vltCollection.Class)}/{GetName(vltCollection)}";
        }

        public static IReadOnlyList<VltCollection> GetChildren(Database database, VltCollection parent)
        {
            return database.RowManager.GetCollections().Where(c => ReferenceEquals(c.Parent, parent)).ToList();
        }
    }
}
