using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Forms = System.Windows.Forms;

namespace Attribulator.UI.PropertyGrid
{
    public class PrimitiveItem : BaseEditItem, ICommandName
    {
        private VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase prop;

        public PrimitiveItem(IParent parent, string name, VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase prop, int padding) : base(parent, name, padding)
        {
            this.prop = prop;
        }

        public string GetName()
        {
            return this.name;
        }

        public override IConvertible GetValue()
        {
            return this.prop.GetValue();
        }

        public override void SetValue(IConvertible value)
        {
            this.prop.SetValue(value);
        }
    }

    public class PrimitiveBoolItem : BaseBoolItem, ICommandName
    {
        private VaultLib.Core.Types.EA.Reflection.Bool prop;

        public PrimitiveBoolItem(IParent parent, string name, VaultLib.Core.Types.EA.Reflection.Bool prop, int padding) : base(parent, name, padding)
        {
            this.prop = prop;
            this.name = name;
        }

        public string GetName()
        {
            return this.name;
        }

        public override bool GetValue()
        {
            return this.prop.Value;
        }

        public override void SetValue(bool val)
        {
            this.prop.Value = val;
        }
    }

    public class PrimitiveEnumItem : BaseEnumItem, ICommandName
    {
        private VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase prop;
        private string name;

        public PrimitiveEnumItem(IParent parent, string name, VaultLib.Core.Types.EA.Reflection.PrimitiveTypeBase prop, int padding) : base(parent, name, padding)
        {
            this.prop = prop;
            this.name = name;
        }

        public string GetName()
        {
            return this.name;
        }

        public override Enum GetValue()
        {
            return this.prop.GetValue() as Enum;
        }

        public override void SetValue(IConvertible val)
        {
            this.prop.SetValue(val);
        }
    }

    public class BlobItem : Control
    {
        private string name;
        private int padding;
        private VaultLib.Core.Types.Attrib.BaseBlob prop;

        public BlobItem(string name, VaultLib.Core.Types.Attrib.BaseBlob prop, int padding)
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
