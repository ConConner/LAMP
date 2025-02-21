using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using LAMP.Classes;
using System;

namespace LAMP.Controls;

public class RoomViewer : UserControl
{
    public override Image BackgroundImage
    {
        get => base.BackgroundImage;
        set
        {
            base.BackgroundImage = value;
            Size = base.BackgroundImage.Size * zoom;
        }
    }

    public RoomViewer()
    {
        AutoScroll = true;
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SuspendLayout();
        ResumeLayout(false);
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        BackColor = Color.FromArgb(40, 50, 50);
        BackgroundImageLayout = ImageLayout.Stretch;
    }

    #region Fields

    public int Zoom
    {
        get
        {
            return zoom;
        }
        set
        {
            if (zoom == value) return;

            //setting rectangles
            //bad way of rescaling the rectangles, a proper way would be to only scale the rectangles once they get drawn :/
            selRect = ScaleRect(selRect, Zoom, value);
            redRect = ScaleRect(redRect, Zoom, value);
            selectedObject = ScaleRect(selectedObject, Zoom, value);

            zoom = Math.Max(value, 1);
            if (BackgroundImage != null) Size = BackgroundImage.Size * zoom;
        }
    }
    private int zoom = 1;

    /// <summary>
    /// The amount of pixels that represent one in-game <see cref="PixelTileSize"/>. Affected by Zoom.
    /// </summary>
    public int TileSize => PixelTileSize * zoom;

    /// <summary>
    /// The amount of pixels that a tile is made of. Constant.
    /// </summary>
    public int PixelTileSize { get; set; } = 16;

    #endregion

    public bool ShowScreenOutlines { get; set; } = false;
    public bool ShowDuplicateOutlines { get; set; } = true;
    public bool ShowScrollBorders { get; set; } = false;
    public bool ShowObjects { get; set; } = true;

    /// <summary>
    /// Sets the screen ID that is currently selected and highlights all duplicates of it
    /// </summary>
    public int SelectedScreen
    {
        get => selectedScreen;
        set
        {
            if (selectedScreen == value) return;
            selectedScreen = value;

            //else a new screen was selected
            if (UniqueScreen.Count != 0)
                foreach (Rectangle r in UniqueScreen)
                {
                    Rectangle result = new(r.X * zoom, r.Y * zoom, r.Width * zoom, r.Height * zoom);
                    Invalidate(result);
                }

            //Populating new list
            UniqueScreen.Clear();
            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    Rectangle rect = new Rectangle(256 * i + 2, 256 * j + 2, 252, 252);
                    int nr = j * 16 + i;
                    if (Globals.Areas[Globals.SelectedArea].Screens[nr] != selectedScreen)
                        continue;
                    UniqueScreen.Add(rect);
                    rect.X *= zoom;
                    rect.Y *= zoom;
                    rect.Width *= zoom;
                    rect.Height *= zoom;
                    Invalidate(rect);
                }
            }
        }
    }
    private int selectedScreen = 0;
    private List<Rectangle> UniqueScreen { get; } = new List<Rectangle>();

    /// <summary>
    /// The tool that is selected from the Toolbar
    /// </summary>
    public LampTool SelectedTool
    {
        get => selectedTool;
        set
        {
            selectedTool = value;
        }
    }
    private LampTool selectedTool = LampTool.Pen;

    #region Rectangles
    /// <summary>
    /// The rectangle showing the area which is going to be replaced if tiles are placed down.
    /// Will expect coordinates and size as room-pixels
    /// </summary>
    public Rectangle RedRect
    {
        get => redRect;
        set
        {
            if (redRect == value) return;

            Rectangle old = redRect;

            //Adjusting the Rectangle for Zoom
            redRect = RecOp.Add(RecOp.Multiply(value, zoom), -1);

            Invalidate(Editor.UniteRect(redRect, old));
        }
    }
    private Rectangle redRect = new Rectangle(-1, -1, 0, 0);

    /// <summary>
    /// The rectangle showing the selected tiles in the room.
    /// Will expect coordinates and size as room-pixels.
    /// </summary>
    public Rectangle SelRect
    {
        get => selRect;
        set
        {
            if (selRect == value) return;

            Rectangle old = selRect;

            //Adjusting the Rectangle for Zoom
            selRect = RecOp.Add(RecOp.Multiply(value, zoom), -1);

            Invalidate(Editor.UniteRect(selRect, old));
        }
    }
    private Rectangle selRect = new Rectangle(-1, -1, 0, 0);

    /// <summary>
    /// Rectangle for the current held object
    /// </summary>
    public Rectangle HeldObject
    {
        get => heldObject;
        set
        {
            if (heldObject == value) return;

            Rectangle old = heldObject;
            heldObject = value;

            Invalidate(Editor.UniteRect(heldObject, old));
}
    }
    private Rectangle heldObject = new Rectangle(-1, -1, -1, -1);
    #endregion

    /// <summary>
    /// Rectangle for the currently 
    /// </summary>
    private Rectangle selectedObject;
    public Rectangle SelectedObject
    {
        get => selectedObject;
        set
        {
            if (selectedObject == value) return;

            Rectangle old = selectedObject;
            selectedObject = value;

            Invalidate(Editor.UniteRect(selectedObject, old));
        }
    }


    #region Pens
    /// <summary>
    /// Pen for the selection rectangle
    /// </summary>
    private Pen SelectionPen { get; set; } = new Pen(Globals.SelectionColor, 1);

    /// <summary>
    /// Pen for screen outlines
    /// </summary>
    private Pen ScreenPen { get; set; } = new Pen(Color.White, 4)
    {
        Alignment = PenAlignment.Inset
    };

    /// <summary>
    /// Pen for the unique screen outlines
    /// </summary>
    private Pen UniqueScreenPen { get; set; } = new Pen(Globals.UniqueScreenColor, 2)
    { 
        Alignment = PenAlignment.Inset
    };

    /// <summary>
    /// Pen for the scroll outlines
    /// </summary>
    private Pen BorderOutlinePen { get; set; } = new Pen(Globals.BorderColor, 20)
    {
        Alignment = PenAlignment.Inset
    };

    /// <summary>
    /// Just a black Pen
    /// </summary>
    private Pen BlackPen { get; set; } = new Pen(Color.Black, 1);

    /// <summary>
    /// Pen for the tile placement outlines
    /// </summary>
    private Pen TilePen { get; set; } = new Pen(Globals.SelectedColor, 1);

    //Objects
    /// <summary>
    /// Pen for Objects
    /// </summary>
    private Pen ObjectPen { get; set; } = new Pen(Globals.ObjectColor, 2)
    {
        Alignment = PenAlignment.Inset
    };

    private Pen ObjectSelectionPen { get; set; } = new Pen(Globals.ObjectSelection, 1);
    #endregion

    public void ResetSelection()
    {
        RedRect = new Rectangle(-1, -1, 0, 0);
        SelRect = new Rectangle(-1, -1, 0, 0);
    }

    public Rectangle ScaleRect(Rectangle rec, int oldZoom, int newZoom)
    {
        Rectangle standardRect = new Rectangle(rec.X / oldZoom, rec.Y / oldZoom, (rec.Width + 1) / oldZoom, (rec.Height + 1) / oldZoom);
        return new Rectangle(standardRect.X * newZoom, standardRect.Y * newZoom, standardRect.Width * newZoom - 1, standardRect.Height * newZoom - 1);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        //change render settings
        e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

        if (redRect.X != -1 && MainWindow.EditingTiles) e.Graphics.DrawRectangle(TilePen, redRect);

        //screen outlines
        if (ShowScreenOutlines)
        {
            for (int i = 0; i < 16; i++)
            {
                //drawing lines every 16 pixels
                e.Graphics.DrawLine(ScreenPen, 256 * i * zoom, 0, 256 * i * zoom, 256 * 16 * zoom); //Vertical
                e.Graphics.DrawLine(ScreenPen, 0, 256 * i * zoom, 256 * 16 * zoom, 256 * i * zoom); //Horizontal
            }
        }

        //scroll borders
        if (ShowScrollBorders)
        {
            foreach (Rectangle r in Globals.ScrollBorders)
            {
                //e.Graphics.DrawLine(BorderOutlinePen, l.Item1.X * zoom, l.Item1.Y * zoom, l.Item2.X * zoom, l.Item2.Y * zoom);
                e.Graphics.DrawRectangle(BorderOutlinePen, RecOp.Multiply(r, zoom));
            }
        }

        //Draw Unique Screen outlines
        if (UniqueScreen.Count != 0 && ShowDuplicateOutlines)
        {
            foreach (Rectangle r in UniqueScreen)
            {
                //Updating sizes
                Rectangle result = new Rectangle(r.X * zoom, r.Y * zoom, r.Width * zoom, r.Height * zoom);
                e.Graphics.DrawRectangle(UniqueScreenPen, result);
            }
        }

        //Draw held object
        if (MainWindow.heldObject != null)
        {
            e.Graphics.DrawEllipse(ObjectPen, heldObject);
        }

        //Draw outline for selected object
        e.Graphics.DrawRectangle(ObjectSelectionPen, selectedObject);
        

        //Draw objects
        if (ShowObjects)
        {
            for(int i = 0; i < 256; i++)
            {
                int screen = i + 256 * Globals.SelectedArea;
                if (Globals.Objects[screen].Count == 0) continue;

                foreach (Enemy o in Globals.Objects[screen]) 
                {
                    Point p = o.GetPosition(i);
                    Rectangle rec = new Rectangle(p.X, p.Y, 16, 16);
                    ObjectPen.Width = 2 * zoom;
                    e.Graphics.DrawEllipse(ObjectPen, RecOp.Add(RecOp.Multiply(rec, zoom), -1));
                    //e.Graphics.DrawRectangle(ObjectPen, rec);
                    //TODO: Add option to switch been circle and rect
                }
            }
        }

        SelectionPen.DashPattern = BlackPen.DashPattern = new float[] { 2, 3 };
        BlackPen.DashOffset = 2;
        e.Graphics.DrawRectangle(BlackPen, selRect);
        e.Graphics.DrawRectangle(SelectionPen, selRect);
        base.OnPaint(e);
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        pevent.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
        pevent.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        base.OnPaintBackground(pevent);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (this.VScroll && (Control.ModifierKeys & Keys.Shift) == Keys.Shift)
        {
            this.VScroll = false;
            base.OnMouseWheel(e);
            this.VScroll = true;
        }
        else
        {
            base.OnMouseWheel(e);
        }
    }
}