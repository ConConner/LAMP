using LAMP.Utilities;
using System;
using System.Windows.Forms;

namespace LAMP.FORMS
{
    public partial class Converter : Form
    {
        public Converter()
        {
            InitializeComponent();
            this.TopLevel = false;
        }

        private bool _edited = false;

        private void txb_hex_input_TextChanged(object sender, EventArgs e)
        {
            if (_edited)
            {
                _edited = false;
                return;
            }
            _edited = true;
            txb_hex_result.Text = Format.StringToInt(txb_hex_input.Text).ToString();
            TextBox txb = (TextBox)sender;
            txb.SelectionStart = txb.Text.Length;
            txb.SelectionLength = 0;
        }

        private void txb_hex_result_TextChanged(object sender, EventArgs e)
        {
            if (_edited)
            {
                _edited = false;
                return;
            }
            _edited = true;
            txb_hex_input.Text = Format.IntToString(Format.StringToDecimalInt(txb_hex_result.Text));
            TextBox txb = (TextBox)sender;
            txb.SelectionStart = txb.Text.Length;
            txb.SelectionLength = 0;
        }
    }
}
