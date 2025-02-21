using System.Drawing;
using System.Windows.Forms;

namespace LAMP.Controls.Overrides;

public class FixedScrollPanel : Panel
{
    protected override Point ScrollToControl(Control activeControl)
    {
        return this.DisplayRectangle.Location;
    }
}
