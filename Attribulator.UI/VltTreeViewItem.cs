using Attribulator.UI.PropertyGrid;
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

            var contextMenu = new ContextMenu();

            var menuItem = new MenuItem();
            menuItem.Header = "Add";
            menuItem.Click += (s, e) =>
            {
                var newNodeWindow = new NewNodeNameWindow();
                newNodeWindow.ShowDialog();
                var result = newNodeWindow.Result;
                if (!string.IsNullOrEmpty(result))
                {
                    string command = $"add_node {this.Collection.Class.Name} {this.Collection.Name} {result}";
                    MainWindow.Instance.ExecuteScriptInternal(new[] { command });
                    MainWindow.Instance.AddScriptLine(command);
                    MainWindow.Instance.PopulateTreeView();
                }
            };
            contextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Header = "Rename";
            menuItem.Click += (s, e) =>
            {
                new CollectionRenameWindow(this.Collection).ShowDialog();
                this.Header = this.Collection.Name;
            };
            contextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Header = "Edit fields";
            menuItem.Click += (s, e) =>
            {
                new EditFieldsWindow(this.Collection).ShowDialog();
            };
            contextMenu.Items.Add(menuItem);

            this.ContextMenu = contextMenu;
        }
    }

    public class ClassTreeViewItem : TreeViewItem
    {
        private VltClass cls;

        public ClassTreeViewItem(VltClass cls)
        {
            this.cls = cls;

            var contextMenu = new ContextMenu();

            var menuItem = new MenuItem();
            menuItem.Header = "Add";
            menuItem.Click += (s, e) =>
            {
                var newNodeWindow = new NewNodeNameWindow();
                newNodeWindow.ShowDialog();
                var result = newNodeWindow.Result;
                if(!string.IsNullOrEmpty(result))
                {
                    string command = $"add_node {this.cls.Name} {result}";
                    MainWindow.Instance.ExecuteScriptInternal(new[] { command });
                    MainWindow.Instance.AddScriptLine(command);
                    MainWindow.Instance.PopulateTreeView();
                }
            };
            contextMenu.Items.Add(menuItem);

            this.ContextMenu = contextMenu;
        }
    }
}
