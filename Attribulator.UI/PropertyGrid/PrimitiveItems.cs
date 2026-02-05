using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Forms = System.Windows.Forms;

namespace Attribulator.UI.PropertyGrid
{
    public class PrimitiveItem : BaseEditItem, ICommandName
    {
        private readonly Func<object> getValue;
        private readonly Action<object> setValue;

        public PrimitiveItem(IParent parent, string name, Func<object> getValue, Action<object> setValue,
            int padding) : base(parent, name, padding)
        {
            this.getValue = getValue;
            this.setValue = setValue;
        }

        public string GetName()
        {
            return this.name;
        }

        public override object GetValue() => this.getValue();

        public override void SetValue(object value) => this.setValue(value);
    }

    public class PrimitiveBoolItem : BaseBoolItem, ICommandName
    {
        private readonly Func<bool> getValue;
        private readonly Action<bool> setValue;

        public PrimitiveBoolItem(IParent parent, string name, Func<bool> getValue, Action<bool> setValue, int padding)
            : base(parent, name, padding)
        {
            this.getValue = getValue;
            this.setValue = setValue;
            this.name = name;
        }

        public string GetName()
        {
            return this.name;
        }

        public override bool GetValue() => this.getValue();

        public override void SetValue(bool val) => this.setValue(val);
    }

    public class PrimitiveEnumItem : BaseEnumItem, ICommandName
    {
        private readonly Func<Enum> getValue;
        private readonly Action<object> setValue;
        private string name;

        public PrimitiveEnumItem(IParent parent, string name, Func<Enum> getValue, Action<object> setValue,
            int padding) : base(parent, name, padding)
        {
            this.getValue = getValue;
            this.setValue = setValue;
            this.name = name;
        }

        public string GetName()
        {
            return this.name;
        }

        public override Enum GetValue() => this.getValue();

        public override void SetValue(object val) => this.setValue(val);
    }

    public class BlobItem : Control
    {
        private string name;
        private int padding;
        private BaseBlob prop;

        public BlobItem(string name, BaseBlob prop, int padding)
        {
            this.name = name;
            this.padding = padding;
            this.prop = prop;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var textBlock = this.GetTemplateChild("PART_TextBlock") as TextBlock;
            textBlock.Text = this.name;
            textBlock.Padding = new Thickness(this.padding, 0, 0, 0);

            var dataLengthTextBlock = this.GetTemplateChild("PART_DataLength") as TextBlock;
            int dataLength = this.prop.Data?.Length ?? 0;
            dataLengthTextBlock.Text = $"Lenght: {dataLength}";

            var exportButton = this.GetTemplateChild("PART_ExportButton") as Button;
            exportButton.IsEnabled = dataLength != 0;
            exportButton.Click += (s, e) =>
            {
                using (var dialog = new Forms.SaveFileDialog())
                {
                    dialog.Filter = "Blob|*.blob";
                    dialog.Title = "Export blob";

                    var result = dialog.ShowDialog();

                    if (result == Forms.DialogResult.OK)
                    {
                        File.WriteAllBytes(dialog.FileName, this.prop.Data);
                    }
                }
            };

            var importButton = this.GetTemplateChild("PART_ImportButton") as Button;
            importButton.Click += (s, e) =>
            {
                using (var dialog = new Forms.OpenFileDialog())
                {
                    dialog.Filter = "Blob|*.blob";
                    dialog.Title = "Import blob";

                    Forms.DialogResult result = dialog.ShowDialog();

                    if (result == Forms.DialogResult.OK)
                    {
                        this.prop.Data = File.ReadAllBytes(dialog.FileName);
                        exportButton.IsEnabled = this.prop.Data.Length != 0;
                        dataLengthTextBlock.Text = $"Lenght: {this.prop.Data.Length}";
                    }
                }
            };
        }
    }
}
