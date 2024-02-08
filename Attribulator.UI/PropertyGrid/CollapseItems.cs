using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows;

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

        void RemoveItem();
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

            this.toggleButton = this.GetTemplateChild("PART_ItemToggler") as ToggleButton;
            this.toggleButton.Checked += (s, e) => this.parent.Expand();
            this.toggleButton.Unchecked += (s, e) => this.parent.Collapse();

            var headerText = this.GetTemplateChild("PART_HeaderText") as TextBlock;
            headerText.Text = this.name;

            this.valueTextBlock = this.GetTemplateChild("PART_ValueText") as TextBlock;
            this.valueTextBlock.Text = this.value;

            var paddingColumn = this.GetTemplateChild("PART_Padding") as ColumnDefinition;
            paddingColumn.Width = new GridLength(this.padding, GridUnitType.Pixel);
        }

        public void UpdateValueText(string value)
        {
            this.value = value;
            this.valueTextBlock.Text = this.value;
        }
    }

    public class ArrayCollapseHeader : CollapseHeader
    {
        public ArrayCollapseHeader(IExpandCollapse parent, string headerName, string value, int padding) : base(parent, headerName, value, padding)
        {
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var removeButton = this.GetTemplateChild("PART_RemoveButton") as Button;
            removeButton.Click += (s, e) => { (this.parent as IItemAddRemove).RemoveItem(); };

            var addButton = this.GetTemplateChild("PART_AddButton") as Button;
            addButton.Click += (s, e) => { (this.parent as IItemAddRemove).AddItem(); };
        }
    }

    public class CollapseItem : StackPanel, IExpandCollapse, IParentUpdate
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
