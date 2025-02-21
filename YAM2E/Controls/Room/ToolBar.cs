using System;
using System.Windows.Forms;
using LAMP.Controls.Other;
using LAMP.Properties;
using LAMP.Classes;

namespace LAMP.Controls.Room;

public partial class ToolBar : UserControl
{
    public ToolBar()
    {
        InitializeComponent();
        SelectedTool = LampTool.Pen;
        CheckUndoRedo(null, null);
    }

    #region EVENTS
    public EventHandler onToolSwitched { get; set; }
    public event EventHandler ToolSwitched
    {
        add
        {
            onToolSwitched += value;
        }
        remove
        {
            onToolSwitched -= value;
        }

    }
    public virtual void OnToolSwitched(EventArgs e)
    {
        onToolSwitched?.Invoke(this, e);
    }

    public EventHandler onToolCommandTriggered { get; set; }
    public event EventHandler ToolCommandTriggered
    {
        add
        {
            onToolCommandTriggered += value;
        }
        remove
        {
            onToolCommandTriggered -= value;
        }

    }
    public virtual void OnToolCommandTriggered(EventArgs e)
    {
        onToolCommandTriggered?.Invoke(this, e);
    }
    #endregion

    #region FIELDS
    public LampTool SelectedTool
    {
        get { return selectedTool; }
        set
        {
            switch (value)
            {
                case (LampTool.Pen):
                    btn_pen_Click(null, null);
                    break;
                case (LampTool.Select):
                    btn_select_Click(null, null);
                    break;
                case (LampTool.Fill):
                    btn_fill_Click(null, null);
                    break;
                case (LampTool.Move):
                    btn_move_Click(null, null);
                    break;
            }
        }
    }
    private LampTool selectedTool;

    public LampToolCommand TriggeredCommand
    {
        get { return triggeredCommand; }
    }
    private LampToolCommand triggeredCommand;

    public int MaxZoom { get; set; } = 10;

    public int ZoomLevel
    {
        get => zoomLevel;
        set
        {
            btn_zoom_in.Enabled = true;
            btn_zoom_out.Enabled = true;

            if (value <= 1) btn_zoom_out.Enabled = false;
            if (value >= MaxZoom) btn_zoom_in.Enabled = false;
            zoomLevel = Math.Max(1, Math.Min(MaxZoom, value));
        }
    }
    private int zoomLevel = 1;

    public EditHistory History
    {
        get => history;
        set
        {
            if (value == history) return;
            history = value;
            history.AddedAction += CheckUndoRedo;
            history.UndoRedo += CheckUndoRedo;
        }
    }
    private EditHistory history;
    #endregion

    /// <summary>
    /// Retroactively adds a new Control that wasnt present before.
    /// If <paramref name="newCategory"/> is set to true, a seperator will be put before the control.
    /// </summary>
    public void AddControl(Control control, bool newCategory = false)
    {
        if (newCategory) flw_controls.Controls.Add(new SeperatorVertical());
        flw_controls.Controls.Add(control);
    }

    private void ResetButtonSelected()
    {
        btn_pen.Image = Resources.PenOne;
        btn_select.Image = Resources.SelectXY;
        btn_fill.Image = Resources.Fill;
        btn_move.Image = Resources.Cursor;
    }

    public void SetTools(bool move, bool pen, bool select, bool fill)
    {
        btn_move.Visible = move;
        btn_pen.Visible = pen;
        btn_select.Visible = select;
        btn_fill.Visible = fill;
    }

    public void SetCopyPaste(bool copy, bool paste)
    {
        btn_copy.Visible = copy;
        btn_paste.Visible = paste;

        if (!(copy || paste)) sep_copy.Visible = false;
    }

    public void SetTransform(bool flipH, bool flipV, bool rotateR, bool rotateL)
    {
        btn_flip_hor.Visible = flipH;
        btn_flip_vert.Visible = flipV;
        btn_rotate_left.Visible = rotateL;
        btn_rotate_right.Visible = rotateR;

        if (!(flipH || flipV || rotateL || rotateR)) sep_transform.Visible = false;
    }

    public void SetUndoRedo(bool undo, bool redo)
    {
        btn_undo.Visible = undo;
        btn_redo.Visible = redo;

        sep_undo_redo.Visible = !(undo || redo);
    }

    public void DisableZoomSeperator()
    {
        sep_zoom.Visible = false;
    }

    public void SetZoom(bool zoomIn, bool zoomOut)
    {
        btn_zoom_in.Visible = zoomIn;
        btn_zoom_out.Visible = zoomOut;

        if (!(zoomIn || zoomOut)) sep_zoom.Visible = false;
    }

    public void EnableZoom(bool zoomIn, bool zoomOut)
    {
        btn_zoom_in.Enabled = zoomIn;
        btn_zoom_out.Enabled = zoomOut;
    }

    private void CheckUndoRedo(object sender, EventArgs e)
    {
        btn_undo.Enabled = (History != null && History.canUndo);
        btn_redo.Enabled = (History != null && History.canRedo);
    }


    #region ToolEvents
    private void btn_pen_Click(object sender, EventArgs e)
    {
        ResetButtonSelected();
        btn_pen.Image = Resources.PenOneSelected;

        selectedTool = LampTool.Pen;
        OnToolSwitched(new EventArgs());
    }

    private void btn_select_Click(object sender, EventArgs e)
    {
        ResetButtonSelected();
        btn_select.Image = Resources.SelectXYSelected;

        selectedTool = LampTool.Select;
        OnToolSwitched(new EventArgs());
    }

    private void btn_fill_Click(object sender, EventArgs e)
    {
        ResetButtonSelected();
        btn_fill.Image = Resources.FillSelected;

        selectedTool = LampTool.Fill;
        OnToolSwitched(new EventArgs());
    }

    private void btn_copy_Click(object sender, EventArgs e)
    {
        triggeredCommand = LampToolCommand.Copy;
        OnToolCommandTriggered(new EventArgs());
    }

    private void btn_paste_Click(object sender, EventArgs e)
    {
        triggeredCommand = LampToolCommand.Paste;
        OnToolCommandTriggered(new EventArgs());
    }

    private void btn_flip_hor_Click(object sender, EventArgs e)
    {
        triggeredCommand = LampToolCommand.FlipHorizontal;
        OnToolCommandTriggered(new EventArgs());
    }

    private void btn_flip_vert_Click(object sender, EventArgs e)
    {
        triggeredCommand = LampToolCommand.FlipVertical;
        OnToolCommandTriggered(new EventArgs());
    }

    private void btn_rotate_right_Click(object sender, EventArgs e)
    {
        triggeredCommand = LampToolCommand.RotateRight;
        OnToolCommandTriggered(new EventArgs());
    }

    private void btn_rotate_left_Click(object sender, EventArgs e)
    {
        triggeredCommand = LampToolCommand.RotateLeft;
        OnToolCommandTriggered(new EventArgs());
    }

    private void btn_zoom_in_Click(object sender, EventArgs e)
    {
        triggeredCommand = LampToolCommand.ZoomIn;
        ZoomLevel += 1;
        OnToolCommandTriggered(new EventArgs());
    }

    private void btn_zoom_out_Click(object sender, EventArgs e)
    {
        triggeredCommand = LampToolCommand.ZoomOut;
        ZoomLevel -= 1;
        OnToolCommandTriggered(new EventArgs());
    }

    private void btn_move_Click(object sender, EventArgs e)
    {
        ResetButtonSelected();
        btn_move.Image = Resources.CursorSelected;

        selectedTool = LampTool.Move;
        OnToolSwitched(new EventArgs());
    }

    private void btn_undo_Click(object sender, EventArgs e)
    {
        triggeredCommand = LampToolCommand.Undo;
        OnToolCommandTriggered(new EventArgs());
    }

    private void btn_redo_Click(object sender, EventArgs e)
    {
        triggeredCommand = LampToolCommand.Redo;
        OnToolCommandTriggered(new EventArgs());
    }
}
#endregion