using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using VaultLib.Core.Data;

namespace Attribulator.UI
{
    public class VltTreeViewItem : TreeViewItem
    {
        public VltCollection Collection { get; private set; }

        public VltTreeViewItem(VltCollection collection)
        {
            this.Collection = collection;
        }
    }
}
