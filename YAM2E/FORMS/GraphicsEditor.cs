using LAMP.Classes;
using LAMP.Classes.M2_Data;
using LAMP.Controls;
using LAMP.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LAMP.FORMS;

public partial class GraphicsEditor : Form
{
    #region Constructor
    public GraphicsEditor(Tileset t)
    {
        InitializeComponent();
        construct();
        GraphicsPointer = t.GfxOffset;
        MetatilePointer = Editor.GetMetaPointerFromTable(t.MetatileTable);
        SetTilesets();
        HideDataControl();
    }

    public GraphicsEditor(Pointer Graphics, Pointer Metatiles = null)
    {
        InitializeComponent();
        construct();
        GraphicsPointer = Graphics;
        MetatilePointer = Metatiles;
        SetTilesets();
        HideDataControl();
    }

    public GraphicsEditor()
    {
        InitializeComponent();
        construct();
    }

    private void construct()
    {
        //Adding the TileViewer for Graphics
        pnl_graphics_view.Controls.Add(GraphicsSet);
        GraphicsSet.Location = new Point(3, 3);
        GraphicsSet.MouseDown += new MouseEventHandler(GraphicsSetMouseDown);
        GraphicsSet.MouseMove += new MouseEventHandler(GraphicsSetMouseMove);
        GraphicsSet.MouseUp += new MouseEventHandler(GraphicsSetMouseUp);

        //Adding the TileViewer for Metatiles
        flw_metatile_view.Controls.Add(MetatileSet);
        MetatileSet.MouseDown += new MouseEventHandler(MetatileSetMouseDown);
        MetatileSet.MouseMove += new MouseEventHandler(MetatileSetMouseMove);
        MetatileSet.MouseUp += new MouseEventHandler(MetatileSetMouseUp);

        //Setting zoom
        GraphicsSet.Zoom = toolbar_graphics.ZoomLevel;
    }

    private void HideDataControl()
    {
        pnl_main.Panel1Collapsed = true;
        pnl_main.Panel1.Hide();
        pnl_main.IsSplitterFixed = true;
    }
    #endregion

    #region Fields
    //Data display fields
    private TileViewer GraphicsSet = new TileViewer() { PixelTileSize = 8 };
    private TileViewer MetatileSet = new TileViewer() { PixelTileSize = 8 };
    private GFX LoadedGFX;
    private Metatiles LoadedMeta;
    private bool changedMetaValue = false;
    public int GfxWidth { get; set; } = 16;
    public int GfxHeight { get; set; } = 8;

    //Pointers
    public Pointer GraphicsPointer
    {
        get => graphicsPointer;
        set
        {
            graphicsPointer = value;
        }
    }
    private Pointer graphicsPointer;

    public Pointer MetatilePointer
    {
        get => metatilePointer;
        set
        {
            metatilePointer = value;
        }
    }
    private Pointer metatilePointer;

    //Graphics Editor
    private int selectedColor = 3;
    private bool wholeTilesSelected => (GraphicsSet.SelectedRegion.X % GraphicsSet.PixelTileSize == 0) && (GraphicsSet.SelectedRegion.Y % GraphicsSet.PixelTileSize == 0) &&
        (GraphicsSet.SelectedRegion.Width % GraphicsSet.PixelTileSize == 0) && (GraphicsSet.SelectedRegion.Height % GraphicsSet.PixelTileSize == 0);
    private const int minimumZoom = 4;
    private Point selectionStart;
    private int selectedTilesWidth = 0;
    private int selectedTilesHeight = 0;

    //Metatile Editor
    private List<int> selectedTiles = new List<int>();
    private int middleClickTile = 0xFF;
    #endregion

    #region Methods

    #region Actions
    private void SetTilesets()
    {
        if (GraphicsPointer == null)
        {
            Bitmap b = new(1, 1); //This clears the images
            LoadedGFX = null;
            GraphicsSet.BackgroundImage = b;
            MetatileSet.BackgroundImage = b;
            return;
        }

        LoadedGFX = new GFX(GraphicsPointer, GfxWidth, GfxHeight);
        txb_offset.Text = Format.PointerToString(GraphicsPointer);

        //Graphics View
        LoadedGFX.Draw();
        GraphicsSet.BackgroundImage = LoadedGFX.Image;

        //Metatile View
        if (MetatilePointer != null)
        {
            LoadedMeta = new Metatiles(LoadedGFX, MetatilePointer);
            LoadedMeta.Draw();
            MetatileSet.BackgroundImage = LoadedMeta.Image;
            txb_meta_offset.Text = Format.PointerToString(metatilePointer);
        }
        else
        {
            LoadedMeta = null;
            MetatileSet.BackgroundImage = new Bitmap(1, 1);
        }
    }

    /// <summary>
    /// Fills all neighbouring pixels with the same color to a new color.
    /// If a selection is made, it will check to stay inside
    /// </summary>
    /// <param name="p">The pixel to start the FloodFill</param>
    /// <param name="oldColor">The color that should be replaced</param>
    /// <param name="newColor">The new color</param>
    private void FloodFillPixel(Point p, int oldColor, int newColor)
    {
        //if not neighbouring
        if (!chb_neighbouring.Checked)
        {
            for (int i = 0; i < LoadedGFX.Width * GraphicsSet.PixelTileSize; i++)
            {
                for (int j = 0; j < LoadedGFX.Height * GraphicsSet.PixelTileSize; j++)
                {
                    if (GraphicsSet.SelRect.X >= 0)
                    {
                        Rectangle checkRectangle = GraphicsSet.SelRect;
                        Point checkPoint = new Point(i * GraphicsSet.Zoom, j * GraphicsSet.Zoom);
                        if (!checkRectangle.Contains(checkPoint)) continue;
                    }

                    if (LoadedGFX.GetPixel(i, j) == oldColor) LoadedGFX.SetPixel(i, j, newColor, true);
                }
            }
            return;
        }

        //check if pixel in bounds
        if (p.X < 0 || p.Y < 0 || p.X >= LoadedGFX.Width * GraphicsSet.PixelTileSize || p.Y >= LoadedGFX.Height * GraphicsSet.PixelTileSize) return;
        //check if in selection
        if (GraphicsSet.SelRect.X >= 0)
        {
            Rectangle checkRectangle = GraphicsSet.SelRect;
            Point checkPoint = new Point(p.X * GraphicsSet.Zoom, p.Y * GraphicsSet.Zoom);
            if (!checkRectangle.Contains(checkPoint)) return;
        }

        //check if pixel is supposed to be edited
        if (LoadedGFX.GetPixel(p) != oldColor) return;
        if (LoadedGFX.GetPixel(p) == newColor) return;

        //replace pixel
        LoadedGFX.SetPixel(p, newColor, true);

        //FloodFill in other directions
        FloodFillPixel(new Point(p.X + 1, p.Y), oldColor, newColor);
        FloodFillPixel(new Point(p.X - 1, p.Y), oldColor, newColor);
        FloodFillPixel(new Point(p.X, p.Y + 1), oldColor, newColor);
        FloodFillPixel(new Point(p.X, p.Y - 1), oldColor, newColor);
    }

    private Rectangle SelectionBetweenPoints(Point p1, Point p2, int adjustment = 0)
    {
        if (p2.X <= p1.X) { p2.X -= adjustment; p1.X += adjustment; }
        if (p2.Y <= p1.Y) { p2.Y -= adjustment; p1.Y += adjustment; }

        int width = Math.Abs(p2.X - p1.X);
        int height = Math.Abs(p2.Y - p1.Y);

        int x = Math.Min(p1.X, p2.X);
        int y = Math.Min(p1.Y, p2.Y);

        return new Rectangle(x, y, width - 1, height - 1);
    }

    private void PlaceMetatiles(int x, int y)
    {
        for (int i = 0; i < selectedTilesWidth; i++)
        {
            for (int j = 0; j < selectedTilesHeight; j++)
            {
                LoadedMeta.ChangeMetaTile(x + i, y + j, (byte)selectedTiles[i * selectedTilesHeight + j]);
            }
        }
    }
    #endregion

    #region Graphics View
    private void GraphicsSetMouseDown(object sender, MouseEventArgs e)
    {
        bool shift = (ModifierKeys & Keys.Shift) != 0;

        //NOTE to self: if I ever remake LAMP this should probably be moved in the tile display controls and passed as EventArgs.
        //The currently selected pixel
        Point pixel = new Point(Math.Max(Math.Min(e.X, GraphicsSet.BackgroundImage.Width * GraphicsSet.Zoom - 1), 0) / GraphicsSet.Zoom, Math.Max(Math.Min(e.Y, GraphicsSet.BackgroundImage.Height * GraphicsSet.Zoom - 1), 0) / GraphicsSet.Zoom);
        Point tileNum = new Point(pixel.X / GraphicsSet.PixelTileSize, pixel.Y / GraphicsSet.PixelTileSize); //The number of the tile selected
        Point tile = new Point(tileNum.X * GraphicsSet.PixelTileSize, tileNum.Y * GraphicsSet.PixelTileSize); //The room coordinates of the selected tile

        if (LoadedGFX == null) return;

        switch (toolbar_graphics.SelectedTool)
        {
            case LampTool.Pen:

                if (e.Button == MouseButtons.Right)
                {
                    //Select color of current pixel
                    selectedColor = (int)LoadedGFX?.GetPixel(pixel);

                    //Change the current color display (THIS IS VERY DUMB CODE, SORRY)
                    foreach (Control c in pnl_colors.Controls)
                    {
                        if (c is not Panel) continue;
                        Panel p = c as Panel;
                        string panelColor = p.Tag as string;
                        if (selectedColor == Convert.ToInt32(panelColor)) pnl_current_color.BackColor = p.BackColor;
                    }
                    return;
                }
                if (e.Button != MouseButtons.Left) return;
                if (GraphicsSet.SelRect.X >= 0 && !GraphicsSet.SelRect.Contains(e.Location)) return;

                //place down pixel
                LoadedGFX?.SetPixel(pixel, selectedColor);
                int changedTile = LoadedGFX.GetTileID(pixel.X, pixel.Y);

                //invalidate the the drawn pixel
                GraphicsSet.Invalidate(RecOp.Multiply(new Rectangle(pixel.X, pixel.Y, 1, 1), GraphicsSet.Zoom));

                //update metatiles
                if (LoadedMeta == null) return;
                List<Rectangle> MetaInvalid = LoadedMeta.RedrawTile(changedTile);
                foreach (Rectangle r in MetaInvalid) MetatileSet.Invalidate(RecOp.Multiply(r, MetatileSet.Zoom));

                break;

            case LampTool.Select:

                if (e.Button != MouseButtons.Left) break;

                //check if a selection already exists
                bool resetSelection = false;
                if (GraphicsSet.SelRect.X != -1)
                {
                    GraphicsSet.SelRect = new Rectangle(-1, -1, 0, 0);
                    resetSelection = true;
                }

                //selecting pressed tile
                //on lower zoom levels it will just select the tile as a whole
                if ((GraphicsSet.Zoom <= minimumZoom && !shift) || (GraphicsSet.Zoom > minimumZoom && shift))
                {
                    selectionStart = new Point(tileNum.X * GraphicsSet.TileSize, tileNum.Y * GraphicsSet.TileSize);
                    if (!resetSelection) GraphicsSet.SelRect = new Rectangle(selectionStart.X, selectionStart.Y, GraphicsSet.TileSize - 1, GraphicsSet.TileSize - 1);
                    break;
                }
                //else set Start coordinate of pixel selection
                selectionStart = new Point(pixel.X * GraphicsSet.Zoom, pixel.Y * GraphicsSet.Zoom);
                if (!resetSelection) GraphicsSet.SelRect = new Rectangle(selectionStart.X, selectionStart.Y, GraphicsSet.Zoom - 1, GraphicsSet.Zoom - 1);

                break;

            case LampTool.Fill:

                //FloodFilling from current Pixel
                FloodFillPixel(pixel, LoadedGFX.GetPixel(pixel), selectedColor);

                //update visuals
                LoadedGFX.Draw();
                GraphicsSet.BackgroundImage = LoadedGFX.Image;
                GraphicsSet.Invalidate();

                //update metatiles
                if (LoadedMeta == null) return;
                LoadedMeta.Draw();
                MetatileSet.BackgroundImage = LoadedMeta.Image;
                MetatileSet.Invalidate();

                break;
        }
    }
    private void GraphicsSetMouseMove(object sender, MouseEventArgs e)
    {
        bool shift = (ModifierKeys & Keys.Shift) != 0;

        //The currently selected pixel
        Point pixel = new Point(Math.Max(Math.Min(e.X, GraphicsSet.BackgroundImage.Width * GraphicsSet.Zoom - 1), 0) / GraphicsSet.Zoom, Math.Max(Math.Min(e.Y, GraphicsSet.BackgroundImage.Height * GraphicsSet.Zoom - 1), 0) / GraphicsSet.Zoom);
        Point tileNum = new Point(pixel.X / GraphicsSet.PixelTileSize, pixel.Y / GraphicsSet.PixelTileSize); //The number of the tile selected
        Point tile = new Point(tileNum.X * GraphicsSet.PixelTileSize, tileNum.Y * GraphicsSet.PixelTileSize); //The room coordinates of the selected tile

        //General Mouse moving Code here:


        switch (toolbar_graphics.SelectedTool)
        {
            case LampTool.Pen:

                if (GraphicsSet.Zoom > minimumZoom) GraphicsSet.RedRect = new Rectangle(pixel.X * GraphicsSet.Zoom, pixel.Y * GraphicsSet.Zoom, GraphicsSet.Zoom - 1, GraphicsSet.Zoom - 1);

                if (e.Button != MouseButtons.Left) return;
                if (GraphicsSet.SelRect.X >= 0 && !GraphicsSet.SelRect.Contains(e.Location)) return;

                //Continue only if clicking
                if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Right) return;

                //Place down pixel
                LoadedGFX?.SetPixel(pixel, selectedColor);
                int changedTile = LoadedGFX.GetTileID(pixel.X, pixel.Y);

                //invalidate the drawn pixel
                GraphicsSet.Invalidate(RecOp.Multiply(new Rectangle(pixel.X, pixel.Y, 1, 1), GraphicsSet.Zoom));

                //update metatiles
                if (LoadedMeta == null) return;
                List<Rectangle> MetaInvalid = LoadedMeta.RedrawTile(changedTile);
                foreach (Rectangle r in MetaInvalid) MetatileSet.Invalidate(RecOp.Multiply(r, MetatileSet.Zoom));

                break;

            case LampTool.Select:

                int zoom = GraphicsSet.Zoom;
                Point selectionEnd;

                //selecting pressed tile
                //on lower zoom levels it will just select the tiles as a whole
                if ((GraphicsSet.Zoom <= minimumZoom && !shift) || (GraphicsSet.Zoom > minimumZoom && shift))
                {
                    GraphicsSet.RedRect = new Rectangle(tileNum.X * GraphicsSet.TileSize, tileNum.Y * GraphicsSet.TileSize, GraphicsSet.TileSize - 1, GraphicsSet.TileSize - 1);
                    if (e.Button != MouseButtons.Left || selectionStart.X == -1) break;

                    //adjust selectionStart if holding shift
                    selectionStart = new Point(selectionStart.X / GraphicsSet.TileSize * GraphicsSet.TileSize, selectionStart.Y / GraphicsSet.TileSize * GraphicsSet.TileSize);

                    selectionEnd = new Point((tile.X + GraphicsSet.PixelTileSize) * zoom, (tile.Y + GraphicsSet.PixelTileSize) * zoom);
                    GraphicsSet.SelRect = SelectionBetweenPoints(selectionStart, selectionEnd, GraphicsSet.TileSize);

                    break;
                }
                //else set end coordinate of pixel selection
                GraphicsSet.RedRect = new Rectangle(pixel.X * GraphicsSet.Zoom, pixel.Y * GraphicsSet.Zoom, GraphicsSet.Zoom - 1, GraphicsSet.Zoom - 1);
                if (e.Button != MouseButtons.Left || selectionStart.X == -1) break;

                selectionEnd = new Point((pixel.X + 1) * zoom, (pixel.Y + 1) * zoom);
                GraphicsSet.SelRect = SelectionBetweenPoints(selectionStart, selectionEnd, zoom);

                break;
        }
    }
    private void GraphicsSetMouseUp(object sender, MouseEventArgs e)
    {
        switch (toolbar_graphics.SelectedTool)
        {
            case (LampTool.Select):

                //check if whole tiles are selected
                if (!wholeTilesSelected) break;

                //load selected tiles
                selectedTiles.Clear();
                Rectangle selection = GraphicsSet.SelectedRegion;
                int width = selectedTilesWidth = selection.Width / GraphicsSet.PixelTileSize;
                int height = selectedTilesHeight = selection.Height / GraphicsSet.PixelTileSize;

                for (int i = 0; i < width; i++) //getting the tile ID
                {
                    for (int j = 0; j < height; j++)
                    {
                        int x = selection.X / GraphicsSet.PixelTileSize + i;
                        int y = selection.Y / GraphicsSet.PixelTileSize + j;
                        selectedTiles.Add(y * GfxWidth + x);
                    }
                }

                break;

            default:
                break;
        }
    }
    private void pnl_graphics_view_MouseDown(object sender, MouseEventArgs e) //Clicking outside
    {
        GraphicsSet.SelRect = new Rectangle(-1, -1, 0, 0);
    }
    #endregion

    #region Metatile View
    private void MetatileSetMouseDown(object sender, MouseEventArgs e)
    {
        //The currently selected pixel
        Point pixel = new Point(Math.Max(Math.Min(e.X, MetatileSet.BackgroundImage.Width * MetatileSet.Zoom - 1), 0) / MetatileSet.Zoom, Math.Max(Math.Min(e.Y, MetatileSet.BackgroundImage.Height * MetatileSet.Zoom - 1), 0) / MetatileSet.Zoom);
        Point tileNum = new Point(pixel.X / MetatileSet.PixelTileSize, pixel.Y / MetatileSet.PixelTileSize); //The number of the tile selected
        Point tile = new Point(tileNum.X * MetatileSet.PixelTileSize, tileNum.Y * MetatileSet.PixelTileSize); //The room coordinates of the selected tile

        if (LoadedMeta == null) return;

        switch (toolbar_metatiles.SelectedTool)
        {
            case LampTool.Pen:

                if (selectedTiles.Count == 0) return;
                if (!wholeTilesSelected) return;

                //place down tile
                if (e.Button == MouseButtons.Left)
                {
                    PlaceMetatiles(tileNum.X, tileNum.Y);
                }
                else if (e.Button == MouseButtons.Right)
                {
                    LoadedMeta.GetMetaTile(tileNum.X, tileNum.Y);
                }
                else if (e.Button == MouseButtons.Middle) LoadedMeta.ChangeMetaTile(tileNum.X, tileNum.Y, (byte)middleClickTile);

                //invalidate
                MetatileSet.Invalidate();

                break;
        }
    }
    private void MetatileSetMouseMove(object sender, MouseEventArgs e)
    {
        //The currently selected pixel
        Point pixel = new Point(Math.Max(Math.Min(e.X, MetatileSet.BackgroundImage.Width * MetatileSet.Zoom - 1), 0) / MetatileSet.Zoom, Math.Max(Math.Min(e.Y, MetatileSet.BackgroundImage.Height * MetatileSet.Zoom - 1), 0) / MetatileSet.Zoom);
        Point tileNum = new Point(pixel.X / MetatileSet.PixelTileSize, pixel.Y / MetatileSet.PixelTileSize); //The number of the tile selected
        Point tile = new Point(tileNum.X * MetatileSet.PixelTileSize, tileNum.Y * MetatileSet.PixelTileSize); //The room coordinates of the selected tile

        if (LoadedMeta == null) return;

        switch (toolbar_metatiles.SelectedTool)
        {
            case LampTool.Pen:

                if (!wholeTilesSelected) return;
                else MetatileSet.RedRect = new Rectangle(tileNum.X * MetatileSet.TileSize, tileNum.Y * MetatileSet.TileSize, selectedTilesWidth * MetatileSet.TileSize - 1, selectedTilesHeight * MetatileSet.TileSize);

                if (e.Button == MouseButtons.Left)
                {
                    PlaceMetatiles(tileNum.X, tileNum.Y);
                }
                else if (e.Button == MouseButtons.Middle) LoadedMeta.ChangeMetaTile(tileNum.X, tileNum.Y, (byte)middleClickTile);

                //invalidate
                MetatileSet.Invalidate();

                break;
        }
    }
    private void MetatileSetMouseUp(object sender, MouseEventArgs e) { }
    #endregion

    #region Events
    private void pnl_color_Click(object sender, EventArgs e)
    {
        Panel p = sender as Panel;
        string number = p.Tag as string;
        selectedColor = Convert.ToInt32(number);
        pnl_current_color.BackColor = p.BackColor;
    }

    private void btn_accept_Click(object sender, EventArgs e)
    {
        //Checking if width * height will be out of bounds in ROM
        int width = (int)num_width.Value;
        int height = (int)num_height.Value;

        GraphicsPointer = txb_offset.Text == "" ? null : Format.StringToPointer(txb_offset.Text);
        MetatilePointer = txb_meta_offset.Text == "" ? null : Format.StringToPointer(txb_meta_offset.Text);

        GfxWidth = width;
        GfxHeight = height;

        SetTilesets();
    }

    private void btn_apply_Click(object sender, EventArgs e)
    {
        if (LoadedGFX != null) Editor.AddDataChunk((DataChunk)LoadedGFX);
        if (LoadedMeta != null) Editor.AddDataChunk((DataChunk)LoadedMeta);
    }

    private void txb_hex_input_TextChanged(object sender, EventArgs e)
    {
        middleClickTile = Format.StringToInt(txb_hex_input.Text);
    }

    private void chb_graphics_grid_CheckedChanged(object sender, EventArgs e)
    {
        GraphicsSet.ShowGrid = chb_graphics_grid.Checked;
    }
    private void chb_metatiles_grid_CheckedChanged(object sender, EventArgs e)
    {
        MetatileSet.ShowGrid = chb_metatiles_grid.Checked;
    }

    /// <summary>
    /// Toolbar for the GFX Editor
    /// </summary>
    private void toolbar_graphics_ToolCommandTriggered(object sender, EventArgs e)
    {
        switch (toolbar_graphics.TriggeredCommand)
        {
            case (LampToolCommand.ZoomIn):
            case (LampToolCommand.ZoomOut):
                GraphicsSet.Zoom = toolbar_graphics.ZoomLevel;
                break;
        }
    }
    private void toolbar_graphics_ToolSwitched(object sender, EventArgs e)
    {
        //Hide additional bars
        //pnl_colors.Visible = toolbar_graphics.SelectedTool == LampTool.Pen || toolbar_graphics.SelectedTool == LampTool.Fill;
        chb_neighbouring.Visible = toolbar_graphics.SelectedTool == LampTool.Fill;

        //Remove red rect
        GraphicsSet.RedRect = new Rectangle(0, 0, 0, 0);
    }

    /// <summary>
    /// Toolbar for the Metatile Editor
    /// </summary>
    private void toolbar_metatiles_ToolCommandTriggered(object sender, EventArgs e)
    {
        switch (toolbar_metatiles.TriggeredCommand)
        {
            case (LampToolCommand.ZoomIn):
            case (LampToolCommand.ZoomOut):
                MetatileSet.Zoom = toolbar_metatiles.ZoomLevel;
                break;
        }
    }

    #region IMPORT / EXPORT
    private void btn_import_gfx_Click(object sender, EventArgs e)
    {
        string path = Editor.ShowOpenDialog("Character Graphics (*.chr)|*.chr|Binary File (*.*)|*.*");
        if (!File.Exists(path) || path == string.Empty) return;

        //Reading data from file
        byte[] data = File.ReadAllBytes(path);

        //Checking if file is valid
        //Size
        if (data.Length > 2048)
        {
            MessageBox.Show("Graphics data is too long", "Error", MessageBoxButtons.OK);
            return;
        }


        Editor.AddDataChunk(new DataChunk(GraphicsPointer, data, "Graphics"));
        SetTilesets();
    }

    private void btn_import_meta_Click(object sender, EventArgs e)
    {
        string path = Editor.ShowOpenDialog("Binary File (*.*)|*.*");
        if (!File.Exists(path) || path == string.Empty) return;

        //Reading data from file
        byte[] data = File.ReadAllBytes(path);

        //Checking if file is valid
        //Size
        if (data.Length > 512)
        {
            MessageBox.Show("Metatile data is too long", "Error", MessageBoxButtons.OK);
            return;
        }
        if (data.Length < 512)
        {
            MessageBox.Show("Metatile data is too short", "Error", MessageBoxButtons.OK);
            return;
        }

        Editor.AddDataChunk(new DataChunk(MetatilePointer, data, "MetatileTable"));
        SetTilesets();
    }

    private void btn_export_gfx_Click(object sender, EventArgs e)
    {
        string path = Editor.ShowSaveDialog("Character Graphics (*.chr)|*.chr");
        if (path == string.Empty) return;

        File.WriteAllBytes(path, (byte[])(DataChunk)LoadedGFX);
    }

    private void btn_export_meta_Click(object sender, EventArgs e)
    {
        string path = Editor.ShowSaveDialog("Binary File (*.*)|*.*");
        if (path == string.Empty) return;

        File.WriteAllBytes(path, (byte[])(DataChunk)LoadedMeta);
    }

    private void btn_import_imageClick(object sender, EventArgs e)
    {
        OpenFileDialog tilesetFile = new OpenFileDialog();
        tilesetFile.Filter = "Bitmaps (*.png, *.bmp, *.gif, *.jpeg, *.jpg, *.tif, *.tiff)|*.png;*.bmp;*.gif;*.jpeg;*.jpg;*.tif;*.tiff";
        if (tilesetFile.ShowDialog() != DialogResult.OK) return;

        Bitmap image = new Bitmap(tilesetFile.FileName);

        //Free memory
        image.Dispose();
    }
    #endregion

    #endregion
    #endregion
}
