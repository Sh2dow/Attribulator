using AttribulatorUI;
using System.Windows.Controls;
using VaultLib.Core.Data;

namespace Attribulator.UI
{
    public class CollectionTreeViewItem : TreeViewItem
    {
        public VltCollection Collection { get; private set; }

        public CollectionTreeViewItem(VltCollection collection)
        {
            this.Collection = collection;
            this.Header = collection.Name;
        }
    }

    public class ClassTreeViewItem : TreeViewItem
    {
        public VltClass Class { get; private set; }

        public ClassTreeViewItem(VltClass cls)
        {
            this.Class = cls;
            this.Header = cls.Name;
        }
    }
}
