using System;
using System.Collections.Generic;
using System.Text;
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

            var contextMenu = new ContextMenu();

            contextMenu.Items.Add(new MenuItem { Header = "Edit fields" });

            this.ContextMenu = contextMenu;
        }
    }
}
