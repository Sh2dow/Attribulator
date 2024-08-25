using AttribulatorUI;
using System.ComponentModel;
using System.Windows;
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
                    this.PropertyChanged(this, new PropertyChangedEventArgs(nameof(this.Executed)));
                }
            }
        }

        public int Found { get; set; }

        public SearchSettings Settings { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class TabHeader : Control
    {
        public string Text { get; private set; }

        private TabItem parent;
        private TextBlock textBlock;

        public TabHeader(TabItem parent, string text)
        {
            this.Text = text;
            this.parent = parent;

        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.textBlock = this.GetTemplateChild("PART_Text") as TextBlock;
            this.textBlock.Text = this.Text;

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

        public void SetText(string text)
        {
            this.Text = text;
            this.textBlock.Text = this.Text;
        }
    }

    public class WelcomeScreen : Control
    {

    }

    public class EditFieldItem : Control
    {
        public string FieldName { get; private set; }

        public bool IsChecked { get; private set; }

        public EditFieldItem(string name, bool isChecked)
        {
            this.FieldName = name;
            this.IsChecked = isChecked;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var nameTextBlock = this.GetTemplateChild("PART_Name") as TextBlock;
            nameTextBlock.Text = this.FieldName;

            var checkbox = this.GetTemplateChild("PART_Checkbox") as CheckBox;
            checkbox.IsChecked = this.IsChecked;
            checkbox.Checked += (s, e) => this.IsChecked = true;
            checkbox.Unchecked += (s, e) => this.IsChecked = false;
        }
    }

    public class CommandModel
    {
        public string Line { get; set; }

        public string File { get; set; }

        public long LineNumber { get; set; }
    }

    public class ScriptErrorItem : Control
    {
        private string message;
        private string file;
        private string line;
        private long lineNumber;

        public ScriptErrorItem(string message, string file, long lineNumber, string line)
        {
            this.message = message;
            this.file = file;
            this.line = line;
            this.lineNumber = lineNumber;
        }

        public override void OnApplyTemplate()
        {
            if (string.IsNullOrEmpty(this.file))
            {
                var grid = this.GetTemplateChild("PART_Grid") as Grid;
                grid.ColumnDefinitions[0].Width = new GridLength(0, GridUnitType.Pixel);
            }
            else
            {
                var fileTextBlock = this.GetTemplateChild("PART_File") as TextBlock;
                fileTextBlock.Text = this.file;
                fileTextBlock.ToolTip = this.file;
            }

            var messageTextBlock = this.GetTemplateChild("PART_Message") as TextBlock;
            messageTextBlock.Text = this.message;
            messageTextBlock.ToolTip = this.line;

            var lineNumberTextBlock = this.GetTemplateChild("PART_LineNumber") as TextBlock;
            lineNumberTextBlock.Text = this.lineNumber.ToString();
        }
    }

    public class BaseTag
    {
        public ItemsControl Parent { get; private set; }

        public BaseTag(ItemsControl parent)
        {
            this.Parent = parent;
        }

        public void SetParent(ItemsControl parent)
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
            return (treeViewItem.Tag as ClassTag)?.Class;
        }

        public static VltCollection Collection(this TreeViewItem treeViewItem)
        {
            return (treeViewItem.Tag as CollectionTag)?.Collection;
        }

        public static ItemsControl GetParent(this TreeViewItem treeViewItem)
        {
            return (treeViewItem.Tag as BaseTag).Parent;
        }

        public static void SetParent(this TreeViewItem treeViewItem, TreeViewItem parent)
        {
            (treeViewItem.Tag as BaseTag).SetParent(parent);
        }

        public static string Header(this TreeViewItem treeViewItem)
        {
            return treeViewItem.Header as string;
        }

        public static TreeViewItem GetNextSibling(this TreeViewItem treeViewItem)
        {
            var parent = treeViewItem.GetParent();
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
                    var parent = treeViewItem.GetParent();
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
                                parent = parentTvi.GetParent();
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

        public static void Select(this TreeViewItem treeViewItem)
        {
            treeViewItem.IsSelected = true;
            treeViewItem.BringIntoView();
        }
    }
}
