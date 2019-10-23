using System;
using System.Windows.Forms;
using WtManager.Controls.WtStyle;
using WtManager.Controls.WtStyle.WtConfigurator;

namespace WtManager.VisualItemRenderers
{
    public class VisualCheckboxRenderer : VisualItemRenderer, IDependentStateProvider
    {
        public VisualCheckboxRenderer(IVisualSourceObject source) 
            : base(source) { }

        protected override Control CreateControl()
        {
            var checkbox = new WtCheckBox();
            checkbox.CheckedChanged += this.Checkbox_OnCheckedChanged;
            return checkbox;
        }

        public override void SetValue(object value)
        {
            if (value is bool boolValue)
                ((WtCheckBox)this.Control).Checked = boolValue;
        }

        public override object GetValue()
        {
            return ((WtCheckBox)this.Control).Checked;
        }

        public override bool SetLabel(string text, LabelRendererConfiguration config)
        {
            this.Control.Text = text;
            return true;
        }

        public event Action<string, bool> StateChanged;
        public bool CurrentState => (this.Control as WtCheckBox)?.Checked ?? false;

        private void Checkbox_OnCheckedChanged(object sender, EventArgs eventArgs)
        {
            if (!(sender is WtCheckBox checkbox))
                return;

            this.StateChanged?.Invoke(checkbox.Name, checkbox.Checked);
        }
    }
}