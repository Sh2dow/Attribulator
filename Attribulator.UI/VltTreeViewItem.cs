using AttribulatorUI;
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

            var menuItem = new MenuItem();
            menuItem.Header = "Rename";
            menuItem.Click += (s, e) => {
                new CollectionRenameWindow(this.Collection).ShowDialog();
                this.Header = this.Collection.Name;
            };
            contextMenu.Items.Add(menuItem);

            //menuItem = new MenuItem();
            //contextMenu.Items.Add(menuItem);

            this.ContextMenu = contextMenu;
        }
    }
}
