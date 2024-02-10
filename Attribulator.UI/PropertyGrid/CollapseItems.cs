using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows;
using AttribulatorUI;

namespace Attribulator.UI.PropertyGrid
{
    public interface IExpandCollapse : IParent
    {
        void Expand();

        void Collapse();
    }

    public interface IItemAddRemove : IParent
    {
        void AddItem();

        bool CanAdd();

        void RemoveItem();

        bool CanRemove();
    }

    public class CollapseHeader : Control
    {
        private string name;

        private string value;

        private TextBlock valueTextBlock;

        private ToggleButton toggleButton;

        protected IExpandCollapse parent;

        private int padding;

        public CollapseHeader(IExpandCollapse parent, string headerName, string value, int padding)
        {
            this.name = headerName;
            this.value = value;
            this.parent = parent;
            this.padding = padding;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var contextMenu = this.CreateContextMenu();

            this.toggleButton = this.GetTemplateChild("PART_ItemToggler") as ToggleButton;
            this.toggleButton.Checked += (s, e) => this.parent.Expand();
            this.toggleButton.Unchecked += (s, e) => this.parent.Collapse();

            var headerText = this.GetTemplateChild("PART_HeaderText") as TextBlock;
            headerText.Text = this.name;
            headerText.ContextMenu = contextMenu;

            this.valueTextBlock = this.GetTemplateChild("PART_ValueText") as TextBlock;
            this.valueTextBlock.Text = this.value;
            this.valueTextBlock.ContextMenu = contextMenu;

            var paddingColumn = this.GetTemplateChild("PART_Padding") as ColumnDefinition;
            paddingColumn.Width = new GridLength(this.padding, GridUnitType.Pixel);
        }

        public void UpdateValueText(string value)
        {
            this.value = value;
            this.valueTextBlock.Text = this.value;
        }

        private ContextMenu CreateContextMenu()
        {
            var contextMenu = new ContextMenu();

            var menuItem = new MenuItem();
            menuItem.Header = "Generate command";
            menuItem.Click += (sender, e) => (this.parent as ICommandGenerator)?.GenerateUpdateCommand();
            contextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Header = "Generate all commands";
            menuItem.Click += (sender, e) => MainWindow.Instance.EditGrid.GenerateUpdateCommand();
            contextMenu.Items.Add(menuItem);

            return contextMenu;
        }
    }

    public class ArrayCollapseHeader : CollapseHeader
    {
        protected Button removeButton;
        protected Button addButton;

        public ArrayCollapseHeader(IExpandCollapse parent, string headerName, string value, int padding) : base(parent, headerName, value, padding)
        {
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.removeButton = this.GetTemplateChild("PART_RemoveButton") as Button;
            removeButton.Click += (s, e) => { (this.parent as IItemAddRemove).RemoveItem(); this.UpdateButtons(); };

            this.addButton = this.GetTemplateChild("PART_AddButton") as Button;
            addButton.Click += (s, e) => { (this.parent as IItemAddRemove).AddItem(); this.UpdateButtons(); };

            this.UpdateButtons();
        }

        private void UpdateButtons()
        {
            var p = this.parent as IItemAddRemove;
            this.addButton.IsEnabled = p.CanAdd();
            this.removeButton.IsEnabled = p.CanRemove();
        }
    }

    public class CollapseItem : StackPanel, IExpandCollapse, IParentUpdate, ICommandGenerator
    {
        protected CollapseHeader headerItem;
        protected StackPanel collapsePanel;
        protected object prop;

        public CollapseItem()
        {

        }

        public CollapseItem(object prop, string name, string value, int padding)
        {
            this.prop = prop;
            this.collapsePanel = new StackPanel();
            this.headerItem = new CollapseHeader(this, name, value, padding);

            this.Children.Add(this.headerItem);
            this.Children.Add(this.collapsePanel);

            this.Collapse();
        }

        protected void AddChild(UIElement child)
        {
            this.collapsePanel.Children.Add(child);
        }

        public void Expand()
        {
            this.collapsePanel.Visibility = Visibility.Visible;
        }

        public void Collapse()
        {
            this.collapsePanel.Visibility = Visibility.Collapsed;
        }

        public virtual void Update()
        {
            this.headerItem.UpdateValueText(this.prop.ToString());
        }

        protected void ClearChildren()
        {
            this.collapsePanel.Children.Clear();
        }

        public void GenerateUpdateCommand()
        {
            foreach (var child in this.collapsePanel.Children)
            {
                if (child is ICommandGenerator item)
                {
                    item.GenerateUpdateCommand();
                }
            }
        }
    }

    public class ArrayCollapseItem : CollapseItem
    {
        public ArrayCollapseItem(object prop, string name, string value, int padding)
        {
            this.prop = prop;
            this.collapsePanel = new StackPanel();
            this.headerItem = new ArrayCollapseHeader(this, name, value, padding);

            this.Children.Add(this.headerItem);
            this.Children.Add(this.collapsePanel);

            this.Collapse();
        }
    }
}
