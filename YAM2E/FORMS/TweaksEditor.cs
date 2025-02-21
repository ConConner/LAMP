using System;
using System.Windows.Forms;

namespace LAMP.FORMS;

public partial class TweaksEditor : Form
{
    public static TweaksEditor Current;

    public TweaksEditor()
    {
        InitializeComponent();
        Current = this;
    }

    private void TweaksEditor_SizeChanged(object sender, EventArgs e)
    {
        tbl_tweaks.Width = this.Width - 40;
        tbl_tweaks.Height = this.Height - 85;
    }

    private void tbl_tweaks_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
    {
    }

    private void tbl_tweaks_CellEndEdit(object sender, DataGridViewCellEventArgs e)
    {
        if (tbl_tweaks.CurrentCell == null) return;
        if (tbl_tweaks.CurrentCell.Value == null) return;
        TestLabel.Text = tbl_tweaks.CurrentCell.Value.ToString();
    }
}