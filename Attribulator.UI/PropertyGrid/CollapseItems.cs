using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows;

namespace Attribulator.UI.PropertyGrid
{
    public interface IExpandCollapse
    {
        void Expand();

        void Collapse();
    }

    public class CollapseHeader : Control
    {
        private string name;

        private string value;

        private TextBlock valueTextBlock;

        private ToggleButton toggleButton;

        private IExpandCollapse parent;

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

    public class CollapseItem : StackPanel, IExpandCollapse, IParent
    {
        private CollapseHeader headerItem;
        private StackPanel collapsePanel;
        private object prop;

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
    }
}
