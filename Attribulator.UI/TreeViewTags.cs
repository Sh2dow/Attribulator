using AttribulatorUI;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs(nameof(Executed)));
                }
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

    public class BaseTag
    {
        public ItemsControl Parent { get; private set; }

        public BaseTag(ItemsControl parent)
        {
            this.Parent = parent;
        }
    }

    public class ClassTag : BaseTag
    {
        public VltClass Class { get; private set; }

        public ClassTag(VltClass cls, TreeView parent) : base(parent)
        {
            this.Class = cls;
        }
    }

    public class CollectionTag : BaseTag
    {
        public VltCollection Collection { get; private set; }

        public CollectionTag(VltCollection collection, TreeViewItem parent) : base(parent)
        {
            this.Collection = collection;
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

        public static ItemsControl Parent(this TreeViewItem treeViewItem)
        {
            return (treeViewItem.Tag as BaseTag).Parent;
        }

        public static string Header(this TreeViewItem treeViewItem)
        {
            return treeViewItem.Header as string;
        }

        public static TreeViewItem GetNextSibling(this TreeViewItem treeViewItem)
        {
            var parent = treeViewItem.Parent();
            if (parent != null)
            {
                int indexInParent = parent.Items.IndexOf(treeViewItem);
                indexInParent++;
                if (indexInParent < parent.Items.Count)
                {
                    return parent.Items[indexInParent] as TreeViewItem;
                }
            }

            return null;
        }

        public static TreeViewItem GetNextItem(this TreeViewItem treeViewItem)
        {
            if (treeViewItem.Items.Count > 0)
            {
                return treeViewItem.Items[0] as TreeViewItem;
            }
            else
            {
                var sibling = treeViewItem.GetNextSibling();
                if (sibling != null)
                {
                    return sibling;
                }
                else
                {
                    var parent = treeViewItem.Parent();
                    while (true)
                    {
                        if (parent is TreeViewItem parentTvi)
                        {
                            var parentSibling = parentTvi.GetNextSibling();
                            if (parentSibling != null)
                            {
                                return parentSibling;
                            }
                            else
                            {
                                parent = parentTvi.Parent();
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            return null;
        }
    }
}
