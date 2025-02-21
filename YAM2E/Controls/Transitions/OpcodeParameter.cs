using System.Windows.Forms;

namespace LAMP.Controls.Other;

public partial class OpcodeParameter : UserControl
{
    public OpcodeParameter(string title, bool isInput, bool isList)
    {
        InitializeComponent();

        lbl_title.Text = title;
        if (!isInput)
        {
            txb_parameter.Dispose();
            cbb_parameterList.Dispose();
            return;
        }
        if (isList)
        {
            txb_parameter.Dispose();
            cbb_parameterList.Visible = true;
        }
    }

    public TextBox txb_Parameter => txb_parameter;
    public ComboBox cbb_ParameterList => cbb_parameterList;
}
