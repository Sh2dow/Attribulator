using System.Windows;
using System.Windows.Controls;
using VaultLib.Core.Data;

namespace Attribulator.UI
{
    public class TreeHeader : Control
    {
        private string text;
        private Image imageClosed;
        private Image imageOpened;
        private TextBlock textBlock;

        public TreeHeader(string text)
        {
            this.text = text;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.textBlock = this.GetTemplateChild("PART_TextBlock") as TextBlock;
            this.textBlock.Text = this.text;

            this.imageClosed = this.GetTemplateChild("PART_ImageClosed") as Image;
            this.imageOpened = this.GetTemplateChild("PART_ImageOpened") as Image;
        }

        public void Expand()
        {
            this.imageClosed.Visibility = System.Windows.Visibility.Collapsed;
            this.imageOpened.Visibility = System.Windows.Visibility.Visible;
        }

        public void Collapse()
        {
            this.imageClosed.Visibility = System.Windows.Visibility.Visible;
            this.imageOpened.Visibility = System.Windows.Visibility.Collapsed;
        }

        public void SetName(string name)
        {
            if (this.textBlock != null)
            {
                this.text = name;
                this.textBlock.Text = name;
            }
        }
    }

    public class BaseTreeViewItem : TreeViewItem
    {
        private TreeHeader treeHeader;

        public BaseTreeViewItem(string name)
        {
            this.treeHeader = new TreeHeader(name);
            this.Header = this.treeHeader;

            this.Expanded += this.Expand;
            this.Collapsed += this.Collapse;
        }

        private void Expand(object sender, RoutedEventArgs e)
        {
            if (this == e.Source && this.Items.Count > 0)
            {
                this.treeHeader.Expand();
            }
        }

        private void Collapse(object sender, RoutedEventArgs e)
        {
            if (this == e.Source)
            {
                this.treeHeader.Collapse();
            }
        }

        public void SetName(string name)
        {
            this.treeHeader.SetName(name);
        }
    }

    public class CollectionTreeViewItem : BaseTreeViewItem
    {
        public VltCollection Collection { get; private set; }

        public CollectionTreeViewItem(VltCollection collection) : base(collection.Name)
        {
            this.Collection = collection;
        }
    }

    public class ClassTreeViewItem : BaseTreeViewItem
    {
        public VltClass Class { get; private set; }

        public ClassTreeViewItem(VltClass cls) : base(cls.Name)
        {
            this.Class = cls;
        }
    }
}
