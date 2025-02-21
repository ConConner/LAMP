using LAMP.Classes.M2_Data.GBC;
using System;
using System.Windows.Forms;

namespace LAMP.Controls;

public partial class PaletteRow : UserControl
{
    public PaletteRow(int row, GBColor[] colorRow)
    {
        InitializeComponent();

        Row = row;
        c0 = colorRow[0];
        c1 = colorRow[1];
        c2 = colorRow[2];
        c3 = colorRow[3];
    }

    //The custom colors from the color dialog
    private int[] customColors = null;

    public int Row
    {
        get => row;
        set
        {
            if (row == value) return;
            row = value;
            lbl_row.Text = $"Row {row}:";
        }
    }
    private int row = 0;

    public event EventHandler ColorChanged;

    private void pnl_col_Click(object sender, EventArgs e)
    {
        Panel p = sender as Panel;
        ColorDialog cd = new ColorDialog()
        {
            AllowFullOpen = true,
            FullOpen = true,
            Color = p.BackColor,
            CustomColors = customColors
        };
        if (cd.ShowDialog() == DialogResult.Cancel) return;
        customColors = cd.CustomColors;
        p.BackColor = GBColor.FromRGB(cd.Color).ToRGB();
        ColorChanged?.Invoke(this, new EventArgs());
    }

    public GBColor c0
    {
        get => GBColor.FromRGB(pnl_col0.BackColor);
        set => pnl_col0.BackColor = value.ToRGB();
    }

    public GBColor c1
    {
        get => GBColor.FromRGB(pnl_col1.BackColor);
        set => pnl_col1.BackColor = value.ToRGB();
    }

    public GBColor c2
    {
        get => GBColor.FromRGB(pnl_col2.BackColor);
        set => pnl_col2.BackColor = value.ToRGB();
    }

    public GBColor c3
    {
        get => GBColor.FromRGB(pnl_col3.BackColor);
        set => pnl_col3.BackColor = value.ToRGB();
    }
}
