using System.Windows.Forms;
using LAMP.Classes;
using System;
using System.Drawing;

namespace LAMP.FORMS;

public partial class ScreenSettings : Form
{
    public ScreenSettings Current;
    private MainWindow ParentWindow;

    //Screen Data
    int SelectedBank;
    int SelectedScreen;
    int UsedScreen;
    int Transition;
    bool PriorityData;

    int ScrollData;
    bool ScrollUp;
    bool ScrollDown;
    bool ScrollLeft;
    bool ScrollRight;

    public ScreenSettings(int AreaIndex = 0, int ScreenIndex = 0, MainWindow Parent = null)
    {
        Current = this;
        InitializeComponent();

        DisableApply();

        //Setting data bindings
        cbb_scse_transition_index.AddNumberedListContent(Globals.Transitions);
        cbb_scse_transition_index.AutoSize();
        cbb_scse_screen_used.DataSource = Globals.ComboboxScreensUsed;
        cbb_scse_screen.DataSource = Globals.ComboboxScreens;

        cbb_scse_screen.SelectedIndex = ScreenIndex;
        cbb_scse_area_bank.SelectedIndex = AreaIndex;
        ParentWindow = Parent;
    }

    /// <summary>
    /// If the selected area or screen changes the values have to be updated
    /// </summary>
    void SelectedScreenChanged()
    {
        SelectedBank = Math.Max(SelectedBank, 0);
        SelectedScreen = Math.Max(SelectedScreen, 0);

        //Used screen data
        int usedScreen = Globals.Areas[SelectedBank].Screens[SelectedScreen];
        if (usedScreen < 0) cbb_scse_screen_used.Text = "Undefined"; //TODO: never happens because some bug
        else
        {
            UsedScreen = usedScreen;
            cbb_scse_screen_used.SelectedIndex = UsedScreen;
        }

        //Scroll data
        int scrollData = Globals.Areas[SelectedBank].Scrolls[SelectedScreen];
        ScrollData = scrollData;
        num_scse_scroll_data.Value = ScrollData;
        UpdateScrollButtons();

        //Transition data
        int transitionData = Globals.Areas[SelectedBank].Tansitions[SelectedScreen];
        Transition = transitionData;
        cbb_scse_transition_index.SelectedIndex = Transition;

        //Priority bit
        PriorityData = Globals.Areas[SelectedBank].Priorities[SelectedScreen];
        chb_samus_priority.Checked = PriorityData;

        SetOlds();
        DisableApply();
    }

    void EnableApply()
    {
        btn_scse_apply.Enabled = true;
    }

    void DisableApply()
    {
        btn_scse_apply.Enabled = false;
    }

    void UpdateScrollButtons()
    {
        byte data = (byte)num_scse_scroll_data.Value;
        ScrollRight = !ByteOp.IsBitSet(data, 0);
        ScrollLeft = !ByteOp.IsBitSet(data, 1);
        ScrollUp = !ByteOp.IsBitSet(data, 2);
        ScrollDown = !ByteOp.IsBitSet(data, 3);

        if (ScrollUp) btn_scroll_up.BackColor = Color.YellowGreen;
        else btn_scroll_up.BackColor = Color.Salmon;
        if (ScrollDown) btn_scroll_down.BackColor = Color.YellowGreen;
        else btn_scroll_down.BackColor = Color.Salmon;
        if (ScrollLeft) btn_scroll_left.BackColor = Color.YellowGreen;
        else btn_scroll_left.BackColor = Color.Salmon;
        if (ScrollRight) btn_scroll_right.BackColor = Color.YellowGreen;
        else btn_scroll_right.BackColor = Color.Salmon;
    }

    /// <summary>
    /// Copies current values to old-variables for comparison
    /// </summary>
    void SetOlds()
    {
        UsedScreen = cbb_scse_screen_used.SelectedIndex;
        ScrollData = (int)num_scse_scroll_data.Value;
        Transition = cbb_scse_transition_index.SelectedIndex;
        PriorityData = chb_samus_priority.Checked;
    }

    private void cbb_scse_area_bank_SelectedIndexChanged(object sender, System.EventArgs e)
    {
        SelectedBank = cbb_scse_area_bank.SelectedIndex;
        SelectedScreenChanged();
    }

    private void cbb_scse_screen_SelectedIndexChanged(object sender, System.EventArgs e)
    {
        SelectedScreen = cbb_scse_screen.SelectedIndex;
        SelectedScreenChanged();
    }

    private void btn_scse_edit_transition_Click(object sender, EventArgs e)
        => new TransitionsEditor(cbb_scse_transition_index.SelectedIndex).Show();

    private void cbb_scse_screen_used_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (cbb_scse_screen_used.SelectedIndex != UsedScreen) EnableApply();
    }

    private void num_scse_scroll_data_ValueChanged(object sender, EventArgs e)
    {
        if ((int)num_scse_scroll_data.Value != ScrollData) EnableApply();
        UpdateScrollButtons();
    }

    private void cbb_scse_transition_index_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (cbb_scse_transition_index.SelectedIndex != Transition) EnableApply();
    }

    private void chb_samus_priority_CheckedChanged(object sender, EventArgs e)
    { 
        if (chb_samus_priority.Checked != PriorityData) EnableApply();
    }

    private void btn_scse_close_Click(object sender, EventArgs e)
        => this.Close();

    private void btn_scse_apply_Click(object sender, EventArgs e)
    {
        //Writing data
        //Used screen data
        int usedScreen = cbb_scse_screen_used.SelectedIndex;
        Globals.Areas[SelectedBank].Screens[SelectedScreen] = usedScreen;

        //Scroll data
        Globals.Areas[SelectedBank].Scrolls[SelectedScreen] = (byte)num_scse_scroll_data.Value;

        //Transition Data and Priority bit
        Globals.Areas[SelectedBank].Tansitions[SelectedScreen] = cbb_scse_transition_index.SelectedIndex;
        Globals.Areas[SelectedBank].Priorities[SelectedScreen] = chb_samus_priority.Checked;

        //updating scroll bounds
        Editor.GetScrollBorders();

        //updating screen
        int ScreenX = SelectedScreen % 16;
        int ScreenY = SelectedScreen / 16;
        int zoom = MainWindow.Room.Zoom;

        Graphics g = Graphics.FromImage(MainWindow.Room.BackgroundImage);
        g.DrawImage(Globals.Screens[SelectedBank][cbb_scse_screen_used.SelectedIndex].Image, new Point(256 * ScreenX, 256 * ScreenY));
        g.Dispose();
        MainWindow.Room.Invalidate(new Rectangle(256 * ScreenX * zoom, 256 * ScreenY * zoom, 256 * zoom, 256 * zoom));

        //Done writing
        DisableApply();
        SetOlds();
    }

    private void btn_scroll_up_Click(object sender, EventArgs e)
        => num_scse_scroll_data.Value = ByteOp.FlipBit((byte)num_scse_scroll_data.Value, 2);

    private void btn_scroll_down_Click(object sender, EventArgs e)
        => num_scse_scroll_data.Value = ByteOp.FlipBit((byte)num_scse_scroll_data.Value, 3);

    private void btn_scroll_left_Click(object sender, EventArgs e)
        => num_scse_scroll_data.Value = ByteOp.FlipBit((byte)num_scse_scroll_data.Value, 1);

    private void btn_scroll_right_Click(object sender, EventArgs e)
        => num_scse_scroll_data.Value = ByteOp.FlipBit((byte)num_scse_scroll_data.Value, 0);
}