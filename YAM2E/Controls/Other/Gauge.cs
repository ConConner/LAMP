using System;
using System.Drawing;
using System.Windows.Forms;

namespace LAMP.Controls.Other;

public partial class Gauge : UserControl
{
    public Gauge()
    {
        InitializeComponent();
    }

    private double _value = 0;
    /// <summary>
    /// The percentage of the gauge that is filled.
    /// </summary>
    public double Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            _value = Math.Min(value, 1);
            pnlBar.Width = (int)(pnlBackground.Width * _value);
        }
    }

    /// <summary>
    /// The Color used for the bar that shows the filled percentage.
    /// </summary>
    public Color Color
    {
        get => pnlBar.BackColor;
        set { pnlBar.BackColor = value; }
    }

    private void Gauge_SizeChanged(object sender, EventArgs e)
    {
        pnlBar.Width = (int)(pnlBackground.Width * Value);
    }
}
