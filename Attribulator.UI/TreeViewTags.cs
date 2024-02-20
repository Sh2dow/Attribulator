using AttribulatorUI;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using VaultLib.Core.Data;

namespace Attribulator.UI
{
    public class SearchResult : INotifyPropertyChanged
    {
        private bool executed;

        public bool Executed
        {
            get => this.executed;
            set
            {
                this.executed = value;
                this.PropertyChanged(this, new PropertyChangedEventArgs(nameof(Executed)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class TabHeader : Control
    {
        public string Text { get; private set; }

        private TabItem parent;

        public TabHeader(TabItem parent, string text)
        {
            this.Text = text;
            this.parent = parent;

        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var textBlock = this.GetTemplateChild("PART_Text") as TextBlock;
            textBlock.Text = this.Text;

            var closeButton = this.GetTemplateChild("PART_CloseButton") as Button;
            closeButton.Click += (s, e) => MainWindow.Instance.RemoveTab(this.parent);

            this.MouseDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Middle && e.ButtonState == MouseButtonState.Pressed)
                {
                    MainWindow.Instance.RemoveTab(this.parent);
                }
            };
        }
    }

    public class WelcomeScreen : Control
    {

    }

    public class ClassTag
    {
        public VltClass Class { get; private set; }

        public ClassTag(VltClass cls)
        {
            this.Class = cls;
        }
    }

    public class CollectionTag
    {
        public VltCollection Collection { get; private set; }

        public TreeViewItem Parent { get; private set; }

        public CollectionTag(VltCollection collection, TreeViewItem parent)
        {
            this.Collection = collection;
            this.Parent = parent;
        }
    }

    public static class TreeViewItemExtensions
    {
        public static VltClass Class(this TreeViewItem treeViewItem)
        {
            return (treeViewItem.Tag as ClassTag).Class;
        }

        public static VltCollection Collection(this TreeViewItem treeViewItem)
        {
            return (treeViewItem.Tag as CollectionTag).Collection;
        }

        public static TreeViewItem Parent(this TreeViewItem treeViewItem)
        {
            return (treeViewItem.Tag as CollectionTag).Parent;
        }

        public static string Header(this TreeViewItem treeViewItem)
        {
            return treeViewItem.Header as string;
        }
    }
}
