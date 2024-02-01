﻿using System;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using LAMP.Classes;
using LAMP.FORMS;
using LAMP.Controls;
using LAMP.Classes.M2_Data;
using System.Collections.Generic;
using LAMP.Utilities;
using LAMP.Controls.Room;
using System.Linq;
using Microsoft.VisualBasic.Devices;
using LAMP.Controls.Other;
using System.Windows.Forms.VisualStyles;
using System.Text.RegularExpressions;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;
using System.CodeDom;
using System.ComponentModel.Design;

namespace LAMP;

public partial class MainWindow : Form
{
    public static MainWindow Current;

    #region FIELDS
    //Tile Viewer vars
    public static TileViewer Tileset = new TileViewer();
    public static RoomViewer Room = new RoomViewer();

    private static RecentFiles Recent = new RecentFiles();
    private static Label TitleLabel = new Label();
    private static Converter PermaConverter = new Converter()
    {
        TopLevel = false,
        Dock = DockStyle.Top,
        FormBorderStyle = FormBorderStyle.None
    };

    /// <summary>
    /// Point where a selection started. Only for temporary storage,
    /// since it can be used by either the room or the tileset
    /// </summary>
    private Point SelectionStart = new Point(-1, -1);
    /// <summary>
    /// The top, left coordinates of the currently selected tile in the tileset
    /// </summary>
    private Point TilesetSelectedTile = new Point(-1, -1);
    /// <summary>
    /// The top, left coordinates of the currently selected tile in the room
    /// </summary>
    private Point RoomSelectedTile = new Point(-1, -1);
    /// <summary>
    /// The direct coordinate selected in the room
    /// </summary>
    private Point RoomSelectedCoordinate = new Point(-1, -1);
    private Size RoomSelectedSize = new Size(-1, -1);
    public static Enemy heldObject = null;
    private byte selectedTileId = 0;

    //Main Editor vars
    public static bool EditingTiles { get; set; } = true;
    bool MovedObject = false;
    Point MoveStartPoint;

    //Object Editor
    private Enemy inspectorObject;
    public Enemy InspectorObject
    {
        get => inspectorObject;
        set
        {
            inspectorObject = value;
            if (value == null)
            {
                grp_object_inspector.Visible = false;
                return;
            }
            grp_object_inspector.Visible = true;
            txb_obj_type.Text = Format.IntToString(value.Type);
            txb_object_number.Text = Format.IntToString(value.Number);
        }
    }
    int inspectorObjectScreen = 0;


    //Graphics vars
    private Pointer gfxOffset;
    private Pointer MetatilePointer;
    private Tileset selectedTileset = null;
    #endregion

    #region Constructor
    public MainWindow()
    {
        SuspendLayout();

        Current = this;
        InitializeComponent();

        //Set Fullscreen if closed in fullscreen
        if (Properties.programsettings.Default.startInFullscreen) WindowState = FormWindowState.Maximized;

        //Check for new Version
        Editor.CheckForUpdate();

        //Populating combobox values for screen settings
        for (int i = 0; i < 512; i++)
        {
            Globals.ComboboxTransitionIndex.Add(i.ToString("X3"));
            if (i < 59) Globals.ComboboxScreensUsed.Add(i.ToString("X2"));
            if (i < 256) Globals.ComboboxScreens.Add(i.ToString("X2"));
        }

        //Displaying recent files
        DisplayRecentFiles();

        //Toolbars
        toolbar_tileset.SetTools(false, false, true, false);
        toolbar_tileset.SetCopyPaste(false, false);
        toolbar_tileset.SetTransform(false, false, false, false);

        toolbar_room.SetTransform(false, false, false, false);
        toolbar_room.SetTools(true, true, true, false);

        //Adding Converter to Panel
        pnl_tileset_resize.Panel2.Controls.Add(PermaConverter);

        //Adding custom controls
        #region Tileset
        flw_tileset_view.Controls.Add(Tileset);
        Tileset.BackColor = Globals.ColorBlack;
        Tileset.MouseDown += new MouseEventHandler(Tileset_MouseDown);
        Tileset.MouseMove += new MouseEventHandler(Tileset_MouseMove);
        Tileset.MouseUp += new MouseEventHandler(Tileset_MouseUp);
        #endregion

        #region Room
        flw_main_room_view.Controls.Add(Room);
        Room.BackColor = Globals.ColorBlack;
        Room.MouseDown += new MouseEventHandler(Room_MouseDown);
        Room.MouseMove += new MouseEventHandler(Room_MouseMove);
        Room.MouseUp += new MouseEventHandler(Room_MouseUp);
        Room.MouseDoubleClick += new MouseEventHandler(RoomDoubleClicked);
        #endregion

        ResumeLayout();

        //Hide the test button if building for release
#if DEBUG
        return;
#endif
        btnTest.Dispose();
    }
    #endregion

    /// <summary>
    /// Everything that happens once a project gets loaded
    /// </summary>
    public void ProjectLoaded()
    {
        SuspendLayout();

        ShowMainControls();

        //Enabling either offset or tileset UI
        tls_input.setMode();

        //Setting base UI values
        gfxOffset = new(0x229BC);
        MetatilePointer = new(0x217BC);
        tls_input.SetGraphics(gfxOffset, 9);

        UpdateTileset();
        UpdateRoom();

        #region Tile Viewer
        Tileset.BringToFront();
        Tileset.ResetSelection();
        #endregion

        #region Room Viewer
        cbb_area_bank.SelectedIndex = 0;
        Room.BringToFront();
        Room.ResetSelection();
        #endregion

        pnl_main_window_view.Visible = true;
        pnl_main_window_view.BringToFront();

        ResumeLayout();
    }

    private void ShowMainControls()
    {
        //Disabling recent files
        Recent.Visible = false;
        TitleLabel.Visible = false;
        tool_strip_main_buttons.Visible = true;
        tool_strip_main_buttons.SendToBack();
        tool_strip_image_buttons.Visible = true;
        sts_main_status_bar.Visible = true;

        //Enabling UI
        foreach (ToolStripItem c in tool_strip_image_buttons.Items) c.Enabled = true;
        foreach (ToolStripItem c in tool_strip_main_buttons.Items) c.Enabled = true;
    }

    private void UpdateTileset()
    {
        Pointer gfx = gfxOffset;
        Pointer meta = MetatilePointer;
        if (Globals.LoadedProject != null && Globals.LoadedProject.useTilesets && selectedTileset != null && Globals.Tilesets.Count != 0)
        {
            gfx = selectedTileset.GfxOffset;
            meta = new Pointer(0x8, Editor.ROM.Read16(Editor.ROM.MetatilePointers.Offset + 2 * selectedTileset.MetatileTable));
        }
        Tileset.BackgroundImage?.Dispose(); //Dispose existing tileset bitmap if existent
        GFX tileGfx = new GFX(gfx, 16, 8);                  //Create GFX and metatile object for drawing
        Metatiles metatiles = new Metatiles(tileGfx, meta); //
        Tileset.BackgroundImage = metatiles.Draw();

        //Update global metatile cache for map drawing
        foreach (Bitmap b in Globals.Metatiles)
        {
            b?.Dispose();
        }
        Globals.Metatiles = metatiles.DrawMetatileList();
    }

    private void DisplayRecentFiles()
    {
        //Disabling tool strips
        tool_strip_main_buttons.Visible = false;
        tool_strip_image_buttons.Visible = false;
        sts_main_status_bar.Visible = false;

        //Adding LAMP title
        TitleLabel.Text = "LAMP - Recent files";
        TitleLabel.Font = new Font("Century Gothic", 40, FontStyle.Bold);
        TitleLabel.Height = 60;
        TitleLabel.Dock = DockStyle.Top;
        Controls.Add(TitleLabel);

        //Adding the recent view
        Controls.Add(Recent);
        Recent.BringToFront();
        Recent.Dock = DockStyle.Fill;
    }

    private void UpdateRoom()
    {
        Point p = new Point(0, 0);
        Globals.AreaBank.Dispose();
        Globals.AreaBank = new Bitmap(4096, 4096, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        Editor.DrawAreaBank(Globals.SelectedArea, Globals.AreaBank, p);
        Room.BackgroundImage = Globals.AreaBank;
        Editor.GetScrollBorders();
    }

    private void GetTilesFromTileset()
    {
        RoomSelectedSize = new Size((Tileset.SelRect.Width + 1) / Tileset.Zoom, (Tileset.SelRect.Height + 1) / Tileset.Zoom);
        int x = Tileset.SelRect.X / Tileset.TileSize;
        int y = Tileset.SelRect.Y / Tileset.TileSize;
        int width = (Tileset.SelRect.Width + 1) / Tileset.TileSize;
        int height = (Tileset.SelRect.Height + 1) / Tileset.TileSize;
        Editor.SelectionWidth = width;
        Editor.SelectionHeight = height;
        lbl_main_selection_size.Text = $"Selected Area: {width} x {height}";

        //returns the selected Metatile offsets
        int tilesWide = Tileset.BackgroundImage.Width / 16;
        Editor.SelectedTiles = new byte[width * height];
        int count = 0;
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                int val = (y + i) * tilesWide + j + x;
                Editor.SelectedTiles[count] = (byte)val;
                count++;
            }
        }
    }

    private void GetTilesFromRoom()
    {
        RoomSelectedSize = new Size((Room.SelRect.Width + 1) / Room.Zoom, (Room.SelRect.Height + 1) / Room.Zoom);
        int x = Room.SelRect.X / Room.Zoom;
        int y = Room.SelRect.Y / Room.Zoom;
        int width = (Room.SelRect.Width + 1) / Room.TileSize;
        int height = (Room.SelRect.Height + 1) / Room.TileSize;
        Editor.SelectionWidth = width;
        Editor.SelectionHeight = height;
        lbl_main_selection_size.Text = $"Selected Area: {width} x {height}";

        //returns the selected Metatile offsets
        Editor.SelectedTiles = new byte[width * height];
        int count = 0;
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                byte value = Editor.GetTileFromXY(x + j * 16, y + i * 16, Globals.SelectedArea);
                Editor.SelectedTiles[count] = value;
                count++;
            }
        }

    }

    private void PlaceSelectedTiles(Point tilePosition)
    {
        int x = tilePosition.X;
        int y = tilePosition.Y;

        //generate array with tiles that have to be replaced
        RoomTile[] replaceTiles = new RoomTile[Editor.SelectionWidth * Editor.SelectionHeight];
        int count = 0;
        for (int i = 0; i < Editor.SelectionHeight; i++)
        {
            for (int j = 0; j < Editor.SelectionWidth; j++)
            {
                int tx = x + 16 * j;
                int ty = y + 16 * i;
                RoomTile t = new RoomTile();
                int scrnNr = Editor.GetScreenNrFromXY(tx, ty, Globals.SelectedArea);
                if (scrnNr == -1)
                {
                    replaceTiles[count++] = new RoomTile() { Unused = true };
                    continue;
                }
                t.ScreenNr = scrnNr;
                t.Screen = Globals.Screens[Globals.SelectedArea][t.ScreenNr];
                t.Area = Globals.SelectedArea;
                t.Position = new Point(tx % 256, ty % 256);
                replaceTiles[count++] = t;
            }
        }

        //Writing data
        count = 0;
        List<int> updatedScreens = new List<int>();
        foreach (RoomTile t in replaceTiles)
        {
            if (t.Unused) continue;
            t.ReplaceTile(Editor.SelectedTiles[count]);
            if (!updatedScreens.Contains(t.ScreenNr)) updatedScreens.Add(t.ScreenNr);
            Editor.DrawScreen(Globals.SelectedArea, t.ScreenNr);
            count++;
        }

        //redrawing updated screens
        count = 0;
        Graphics g = Graphics.FromImage(Globals.AreaBank);
        foreach (int nr in Globals.Areas[Globals.SelectedArea].Screens)
        {
            //screen pos
            int sy = count / 16;
            int sx = count % 16;
            sx *= 256;
            sy *= 256;

            if (!updatedScreens.Contains(nr))
            {
                count++;
                continue;
            }
            GameScreen screen = Globals.Screens[Globals.SelectedArea][nr];
            g.DrawImage(screen.Image, new Point(sx, sy));
            Room.Invalidate(new Rectangle(sx * Room.Zoom, sy * Room.Zoom, 256 * Room.Zoom, 256 * Room.Zoom));
            count++;
        }
        g.Dispose();
    }

    private void FloodFill(Point StartPosition)
    {
        List<RoomTile> replaceTiles = new List<RoomTile>();

        //get current tile
        //current Screen
        GameScreen current = Globals.Screens[Globals.SelectedArea][Editor.GetScreenNrFromXY(StartPosition.X, StartPosition.Y, Globals.SelectedArea)];
        int tx = (StartPosition.X % 256) / 16;
        int ty = (StartPosition.Y % 256) / 16;
        int count = ty * 16 + tx;

        int originalID = current.Data[count];

        //Starting the FillSteps
        FillStep(new Point(StartPosition.X, StartPosition.Y), originalID, replaceTiles);

        //List should now have all the tiles to replace
        //Writing data
        count = 0;
        List<int> updatedScreens = new List<int>();
        foreach (RoomTile t in replaceTiles)
        {
            if (t.Unused) continue;
            t.ReplaceTile(Editor.SelectedTiles[0]);
            if (!updatedScreens.Contains(t.ScreenNr)) updatedScreens.Add(t.ScreenNr);
            Editor.DrawScreen(Globals.SelectedArea, t.ScreenNr);
            count++;
        }

        //redrawing updated screens
        count = 0;
        Graphics g = Graphics.FromImage(Globals.AreaBank);
        foreach (int nr in Globals.Areas[Globals.SelectedArea].Screens)
        {
            //screen pos
            int sy = count / 16;
            int sx = count % 16;
            sx *= 256;
            sy *= 256;

            if (!updatedScreens.Contains(nr))
            {
                count++;
                continue;
            }
            GameScreen screen = Globals.Screens[Globals.SelectedArea][nr];
            g.DrawImage(screen.Image, new Point(sx, sy));
            Room.Invalidate(new Rectangle(sx * Room.Zoom, sy * Room.Zoom, 256 * Room.Zoom, 256 * Room.Zoom));
            count++;
        }
        g.Dispose();
    }

    private void FillStep(Point Start, int originalID, List<RoomTile> tileList)
    {
        //get current ID
        GameScreen current = Globals.Screens[Globals.SelectedArea][Editor.GetScreenNrFromXY(Start.X, Start.Y, Globals.SelectedArea)];
        int tx = (Start.X % 256) / 16;
        int ty = (Start.Y % 256) / 16;
        int count = ty * 16 + tx;

        int currentID = current.Data[count];

        if (currentID != originalID) return;

        //Add current tile
        RoomTile t = new RoomTile();
        t.ScreenNr = Editor.GetScreenNrFromXY(Start.X, Start.Y, Globals.SelectedArea);
        t.Screen = current;
        t.Area = Globals.SelectedArea;
        t.Position = new Point(Start.X % 256, Start.Y % 256);
        tileList.Add(t);

        //Doing fill steps in all directions
        FillStep(new Point(Start.X + 16, Start.Y), originalID, tileList);
        FillStep(new Point(Start.X - 16, Start.Y), originalID, tileList);
        FillStep(new Point(Start.X, Start.Y + 16), originalID, tileList);
        FillStep(new Point(Start.X, Start.Y - 16), originalID, tileList);
    }

    private void ToggleScreenOutlines()
    {
        Room.ShowScreenOutlines = !Room.ShowScreenOutlines;
        Room.Invalidate();
    }

    private void ToggleDuplicateOutlines()
    {
        Room.ShowDuplicateOutlines = !Room.ShowDuplicateOutlines;
        Room.Invalidate();
    }

    private void ToggleScrollBorders()
    {
        Room.ShowScrollBorders = !Room.ShowScrollBorders;
        Room.Invalidate();
    }

    private void ToggleSelectionFocus(bool TilesetFocused)
    {
        if (TilesetFocused == true)
        {
            Room.ResetSelection();
            Room.Invalidate();
        }
        else
        {
            Tileset.ResetSelection();
            Tileset.Invalidate();
        }
    }

    public void SwitchTilesetOffsetMode()
    {
        tls_input.setMode();
    }

    public void LoadTilesetList()
    {
        tls_input.populateTilesets();
    }

    private void SetTestSaveValues()
    {
        Save s = Globals.TestROMSave;

        if (Globals.LoadedProject.useTilesets == true && Globals.Tilesets.Count >= 1)
        {
            s.setTilesetID(tls_input.SelectedTileset);
        }
        else
        {
            s.TilesetUsed = -1;

            //setting data manually
            s.TileGraphics = tls_input.GraphicsOffset;
            s.MetatileData = tls_input.MetatilePointer;
            s.MetatileTable = tls_input.MetatileTable;
        }

        //populating the rest of data
        //Position
        int posX = RoomSelectedTile.X;
        int posY = RoomSelectedTile.Y - 16;

        s.MapBank = (byte)(cbb_area_bank.SelectedIndex); // +9 because the Game expects the actual bank and nost just an index
        s.CamScreenX = s.SamusScreenX = (byte)(posX / 256);
        s.CamScreenY = s.SamusScreenY = (byte)(posY / 256);
        s.CamX = s.SamusX = (byte)posX;
        s.CamY = s.SamusY = (byte)posY;
    }

    #region Main Window Events

    #region Tileset Events
    private void Tileset_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Right) return;
        int tileWidth = 16 * Tileset.Zoom;

        ToggleSelectionFocus(true);

        int x = (e.X / tileWidth) * tileWidth; //tile position at moment of click
        int y = (e.Y / tileWidth) * tileWidth; //

        //setting start position for selection
        SelectionStart.X = x;
        SelectionStart.Y = y;

        Tileset.SelRect = new Rectangle(SelectionStart.X, SelectionStart.Y, tileWidth - 1, tileWidth - 1);
    }

    private void Tileset_MouseMove(object sender, MouseEventArgs e)
    {
        int tileWidth = 16 * Tileset.Zoom;

        int x = (e.X / tileWidth) * tileWidth; //locks position of mouse to edge of tiles
        int y = (e.Y / tileWidth) * tileWidth; //

        //Guard clause
        if ((x == TilesetSelectedTile.X && y == TilesetSelectedTile.Y) //same tile still selected
            || (x < 0 || y < 0) //outside of the tileset
            || (x > Tileset.BackgroundImage.Width * Tileset.Zoom - tileWidth || y > Tileset.BackgroundImage.Height * Tileset.Zoom - tileWidth)) //also outside of the tileset
        {
            return;
        }

        TilesetSelectedTile.X = x; //Setting currently selected tile on the tileset
        TilesetSelectedTile.Y = y; //

        if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
        {
            int width = Math.Abs((TilesetSelectedTile.X) - SelectionStart.X) + tileWidth - 1;   //Width and Height of the selected
            int height = Math.Abs((TilesetSelectedTile.Y) - SelectionStart.Y) + tileWidth - 1;  //area on the tileset

            Tileset.RedRect = new Rectangle(-1, 0, 0, 0); //This hides the red Rect
            Tileset.SelRect = new Rectangle(Math.Min(SelectionStart.X, TilesetSelectedTile.X), Math.Min(SelectionStart.Y, TilesetSelectedTile.Y), width, height);

            lbl_main_selection_size.Text = $"Selected Area: {(width + 1) / 16} x {(height + 1) / 16}";
        }
        else //Only update the cursor rectangle
        {
            Tileset.RedRect = new Rectangle(TilesetSelectedTile.X, TilesetSelectedTile.Y, tileWidth - 1, tileWidth - 1);
        }

    }

    private void Tileset_MouseUp(object sender, MouseEventArgs e)
    {
        GetTilesFromTileset();
    }
    #endregion

    #region Room Events
    /*                      Setting coordinate based variables
    * 
    * Due to zooming, coordinates can be a bit confusing. e.X and e.Y will always give the coordinates
    * of the mouse inside of the RoomView, however, those might not always directly translate to
    * coordinates from the game, especially when a zoom is applied. Therefore, there will be coordinates,
    * which are divided by the zoom factor and represent the currently selected pixel in the room.
    */

    private void Room_MouseDown(object sender, MouseEventArgs e)
    {
        Room.Focus();

        //The currently selected pixel
        Point pixel = new Point(Math.Max(Math.Min(e.X, Room.BackgroundImage.Width * Room.Zoom - 1), 0) / Room.Zoom, Math.Max(Math.Min(e.Y, Room.BackgroundImage.Height * Room.Zoom - 1), 0) / Room.Zoom);

        //The number of the tile selected
        Point tileNum = new Point(pixel.X / Room.PixelTileSize, pixel.Y / Room.PixelTileSize);

        //The room coordinates of the selected tile
        Point tile = new Point(tileNum.X * Room.PixelTileSize, tileNum.Y * Room.PixelTileSize);

        //Quick switch for tools
        if (e.Button == MouseButtons.Middle)
        {
            if (toolbar_room.SelectedTool == LampTool.Pen) toolbar_room.SelectedTool = LampTool.Move;
            else toolbar_room.SelectedTool = LampTool.Pen;
        }

        switch (toolbar_room.SelectedTool)
        {
            case (LampTool.Pen): //Used for placing tiles

                if (e.Button == MouseButtons.Left)
                {
                    PlaceSelectedTiles(tile);
                }
                if (e.Button == MouseButtons.Right) //Quick Select
                {
                    //Setting start of selection
                    SelectionStart = tile;
                    Room.SelRect = new Rectangle(SelectionStart.X, SelectionStart.Y, Room.PixelTileSize, Room.PixelTileSize);
                }

                break;

            case (LampTool.Select): //Selecting tiles directly from the room window

                //Setting start of selection
                SelectionStart = tile;
                Room.SelRect = new Rectangle(SelectionStart.X, SelectionStart.Y, Room.PixelTileSize, Room.PixelTileSize);
                if (e.Button == MouseButtons.Right) selectedTileId = Editor.GetTileFromXY(tile.X, tile.Y, cbb_area_bank.SelectedIndex);
                break;

            case (LampTool.Fill): //fill everything in with one tile
                FloodFill(tile);
                break;

            case (LampTool.Move): //Move selected tiles, objects, edit screens and more

                MovedObject = false;
                if (!Room.ShowObjects) break;
                heldObject = Editor.FindObject(pixel.X, pixel.Y, Globals.SelectedArea);
                InspectorObject = heldObject;
                inspectorObjectScreen = Globals.SelectedScreenNr;

                if (InspectorObject == null) Room.SelectedObject = new Rectangle(-1, -1, 1, 1);
                else
                {
                    Point objectLocation = InspectorObject.GetPosition(Globals.SelectedScreenNr);
                    Room.SelectedObject = new Rectangle(objectLocation.X * Room.Zoom, objectLocation.Y * Room.Zoom, Room.TileSize - 1, Room.TileSize - 1);
                }

                if (e.Button == MouseButtons.Right)
                {
                    ctx_btn_edit_object.Enabled = false;
                    ctx_btn_remove_object.Enabled = false;
                    if (heldObject == null) break;
                    ctx_btn_edit_object.Enabled = true;
                    ctx_btn_remove_object.Enabled = true;
                }

                if (heldObject == null) break;

                //Accidental move prevention
                MoveStartPoint = e.Location;

                break;
        }
    }

    private void Room_MouseMove(object sender, MouseEventArgs e)
    {
        //The currently selected pixel
        Point pixel = new Point(Math.Max(Math.Min(e.X, Room.BackgroundImage.Width * Room.Zoom - 1), 0) / Room.Zoom, Math.Max(Math.Min(e.Y, Room.BackgroundImage.Height * Room.Zoom - 1), 0) / Room.Zoom);
        RoomSelectedCoordinate = pixel;

        //The number of the tile selected
        Point tileNum = new Point(pixel.X / Room.PixelTileSize, pixel.Y / Room.PixelTileSize);

        //The room coordinates of the selected tile
        Point tile = new Point(tileNum.X * Room.PixelTileSize, tileNum.Y * Room.PixelTileSize);

        #region General Mouse moving code
        Globals.SelectedScreenX = Math.Min(pixel.X / 256, 15); //screen the mouse cursor is on
        Globals.SelectedScreenY = Math.Min(pixel.Y / 256, 15); //
        Globals.SelectedScreenNr = Globals.SelectedScreenY * 16 + Globals.SelectedScreenX;

        if (Room.SelectedScreen != Globals.Areas[Globals.SelectedArea].Screens[Globals.SelectedScreenNr]) Room.SelectedScreen = Globals.Areas[Globals.SelectedArea].Screens[Globals.SelectedScreenNr];
        lbl_main_hovered_screen.Text = $"Selected Screen: {Globals.SelectedScreenX:X2}, {Globals.SelectedScreenY:X2}";
        lbl_screen_used.Text = $"Used: {Room.SelectedScreen:X2}";
        #endregion

        //Tool specific mouse moving code
        switch (toolbar_room.SelectedTool)
        {
            case (LampTool.Pen): //Used for placing tiles

                if (RoomSelectedTile == tile) break;
                RoomSelectedTile = tile;

                if (e.Button == MouseButtons.Right) //Quick Select
                {
                    int _width = Math.Abs((tile.X) - SelectionStart.X) + Room.PixelTileSize; //Width and Height of the Selection
                    int _height = Math.Abs((tile.Y) - SelectionStart.Y) + Room.PixelTileSize;//

                    Room.RedRect = new Rectangle(-1, 0, 0, 0); //This hides the red Rect
                    Room.SelRect = new Rectangle(Math.Min(SelectionStart.X, tile.X), Math.Min(SelectionStart.Y, tile.Y), _width, _height);

                    lbl_main_selection_size.Text = $"Selected Area: {(_width) / Room.PixelTileSize} x {(_height) / Room.PixelTileSize}";
                }
                else
                {
                    Room.RedRect = new Rectangle(tile.X, tile.Y, RoomSelectedSize.Width, RoomSelectedSize.Height);
                }

                if (e.Button != MouseButtons.Left) break;

                PlaceSelectedTiles(tile);

                break;

            case (LampTool.Select): //Selecting tiles directly from the room window

                if ((e.Button != MouseButtons.Left || e.Button == MouseButtons.Right) || RoomSelectedTile == tile) break;
                RoomSelectedTile = tile;

                int width = Math.Abs((tile.X) - SelectionStart.X) + Room.PixelTileSize; //Width and Height of the Selection
                int height = Math.Abs((tile.Y) - SelectionStart.Y) + Room.PixelTileSize;//

                Room.RedRect = new Rectangle(-1, 0, 0, 0); //This hides the red Rect
                Room.SelRect = new Rectangle(Math.Min(SelectionStart.X, tile.X), Math.Min(SelectionStart.Y, tile.Y), width, height);

                lbl_main_selection_size.Text = $"Selected Area: {(width) / Room.PixelTileSize} x {(height) / Room.PixelTileSize}";

                break;

            case (LampTool.Fill): //fill everything in with one tile
                break;

            case (LampTool.Move): //Move selected tiles, objects, edit screens and more

                if (RoomSelectedTile != tile) RoomSelectedTile = tile;

                //Tool should only work with left click
                if (e.Button != MouseButtons.Left) break;

                ///OBJECT MOVEMENT
                //Accidental movement prevention
                if (MovedObject == false && heldObject != null)
                {
                    //Check if mouse got moved significantly
                    Rectangle check = new Rectangle(MoveStartPoint.X - 2, MoveStartPoint.Y - 2, 4, 4);
                    if (!check.Contains(e.Location))
                    {
                        //This code should only run once
                        MovedObject = true;

                        Editor.RemoveObject(heldObject, Globals.SelectedArea, Globals.SelectedScreenNr);
                        Room.HeldObject = new Rectangle(e.X - Room.TileSize / 2, e.Y - Room.TileSize / 2, Room.TileSize - 1, Room.TileSize - 1);
                    }
                }

                //Moving the held object rectangle
                if (heldObject != null && MovedObject == true)
                {
                    Point objectCoordinate = pixel;

                    if ((ModifierKeys & Keys.Shift) != 0)
                    {
                        objectCoordinate.X = (pixel.X / 16) * 16 + 8;
                        objectCoordinate.Y = (pixel.Y / 16) * 16 + 8;
                    }
                    if ((ModifierKeys & Keys.Alt) != 0)
                    {
                        objectCoordinate.X = ((pixel.X - 8) / 16) * 16 + 16;
                        objectCoordinate.Y = ((pixel.Y - 8) / 16) * 16 + 16;
                    }
                    if (((ModifierKeys & Keys.Shift) != 0) && ((ModifierKeys & Keys.Alt) != 0))
                    {
                        objectCoordinate.X = (pixel.X / 8) * 8 + 8;
                        objectCoordinate.Y = (pixel.Y / 8) * 8 + 8;
                    }

                    objectCoordinate.X *= Room.Zoom;
                    objectCoordinate.Y *= Room.Zoom;

                    Room.SelectedObject = Room.HeldObject = new Rectangle(objectCoordinate.X - Room.TileSize / 2, objectCoordinate.Y - Room.TileSize / 2, Room.TileSize - 1, Room.TileSize - 1);
                    inspectorObjectScreen = Globals.SelectedScreenNr;
                }

                break;
        }
    }

    private void Room_MouseUp(object sender, MouseEventArgs e)
    {
        Room.HeldObject = new Rectangle(-1, -1, 1, 1);

        switch (toolbar_room.SelectedTool)
        {
            case (LampTool.Pen):
                if (e.Button == MouseButtons.Right) GetTilesFromRoom();
                break;
            case (LampTool.Select):
                GetTilesFromRoom();
                break;
        }

        if (heldObject != null && MovedObject != false)
        {
            Point objectCoordinate = RoomSelectedCoordinate;
            if ((ModifierKeys & Keys.Shift) != 0)
            {
                objectCoordinate.X = (RoomSelectedCoordinate.X / 16) * 16 + 8;
                objectCoordinate.Y = (RoomSelectedCoordinate.Y / 16) * 16 + 8;
            }
            if ((ModifierKeys & Keys.Alt) != 0)
            {
                objectCoordinate.X = ((RoomSelectedCoordinate.X - 8) / 16) * 16 + 16;
                objectCoordinate.Y = ((RoomSelectedCoordinate.Y - 8) / 16) * 16 + 16;
            }
            if (((ModifierKeys & Keys.Shift) != 0) && ((ModifierKeys & Keys.Alt) != 0))
            {
                objectCoordinate.X = (RoomSelectedCoordinate.X / 8) * 8 + 8;
                objectCoordinate.Y = (RoomSelectedCoordinate.Y / 8) * 8 + 8;
            }

            heldObject.sX = (byte)(objectCoordinate.X % 256);
            heldObject.sY = (byte)(objectCoordinate.Y % 256);

            Globals.Objects[Globals.SelectedScreenNr + 256 * Globals.SelectedArea].Add(heldObject);
        }
        MovedObject = false;
        heldObject = null;
    }

    private void RoomDoubleClicked(object sender, MouseEventArgs e)
    {
        //The currently selected pixel
        Point pixel = new Point(Math.Max(Math.Min(e.X, Room.BackgroundImage.Width * Room.Zoom - 1), 0) / Room.Zoom, Math.Max(Math.Min(e.Y, Room.BackgroundImage.Height * Room.Zoom - 1), 0) / Room.Zoom);

        //The number of the tile selected
        Point tileNum = new Point(pixel.X / Room.PixelTileSize, pixel.Y / Room.PixelTileSize);

        //The room coordinates of the selected tile
        Point tile = new Point(tileNum.X * Room.PixelTileSize, tileNum.Y * Room.PixelTileSize);

        //Double clicking with Move tool
        if (Room.SelectedTool != LampTool.Move || e.Button != MouseButtons.Left) return;


        ///QUICK EDITS
        //Objects
        Enemy clickedObject = Editor.FindObject(pixel.X, pixel.Y, Globals.SelectedArea);
        if (clickedObject != null && Room.ShowObjects == true)
        {
            //new ObjectSettings(clickedObject).Show();
            return;
        }

        //Scrolls
        //checking if edge is clicked
        if (Room.ShowScrollBorders)
        {
            const int margin = 16;
            Point check = new Point(pixel.X % 256, pixel.Y % 256);
            byte val = Globals.Areas[Globals.SelectedArea].Scrolls[Globals.SelectedScreenNr];
            if (new Rectangle(256 - margin, 0, margin, 256).Contains(check)) val = ByteOp.FlipBit(val, 0); //Right edge
            else if (new Rectangle(0, 0, margin, 256).Contains(check)) val = ByteOp.FlipBit(val, 1); //Left edge
            else if (new Rectangle(0, 0, 256, margin).Contains(check)) val = ByteOp.FlipBit(val, 2); //Top edge
            else if (new Rectangle(0, 256 - margin, 256, margin).Contains(check)) val = ByteOp.FlipBit(val, 3); //Bottom edge
            if (Globals.Areas[Globals.SelectedArea].Scrolls[Globals.SelectedScreenNr] != val) //This triggers if anything got changed
            {
                Globals.Areas[Globals.SelectedArea].Scrolls[Globals.SelectedScreenNr] = val;

                Editor.GetScrollBorders();
                Room.Invalidate(new Rectangle((e.X / (256 * Room.Zoom)) * 256 * Room.Zoom, (e.Y / (256 * Room.Zoom)) * 256 * Room.Zoom, 256 * Room.Zoom, 256 * Room.Zoom));
                return;
            }
        }

        //Screen
        new ScreenSettings(Globals.SelectedArea, Globals.SelectedScreenNr, Current).Show();
    }
    #endregion

    #region toolbars
    private void toolbar_tileset_ToolCommandTriggered(object sender, EventArgs e)
    {
        switch (toolbar_tileset.TriggeredCommand)
        {
            case (LampToolCommand.ZoomIn):
            case (LampToolCommand.ZoomOut):
                Tileset.Zoom = toolbar_tileset.ZoomLevel;
                break;
        }
    }

    private void toolbar_room_ToolCommandTriggered(object sender, EventArgs e)
    {
        switch (toolbar_room.TriggeredCommand)
        {
            case (LampToolCommand.ZoomIn):
            case (LampToolCommand.ZoomOut):
                Room.Zoom = toolbar_room.ZoomLevel;
                break;

            case (LampToolCommand.Copy):
                copyTiles();
                break;

            case (LampToolCommand.Paste):
                pasteTiles();
                break;
        }
    }

    private void toolbar_room_ToolSwitched(object sender, EventArgs e)
    {
        Room.SelectedTool = toolbar_room.SelectedTool;

        Room.ContextMenuStrip = null;
        if (toolbar_room.SelectedTool == LampTool.Move) Room.ContextMenuStrip = ctx_room_context_menu;
        if (toolbar_room.SelectedTool == LampTool.Select) Room.ContextMenuStrip = ctx_select_tool;

        Room.RedRect = Rectangle.Empty;
    }
    #endregion

    #region Main Events
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (Globals.LoadedProject == null) return;

        DialogResult r = MessageBox.Show("Save changes before closing?", "Unsaved changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

        if (r == DialogResult.Yes) Editor.SaveProject();
        e.Cancel = (r == DialogResult.Cancel);
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (Globals.LoadedProject == null) return;

        switch (e.KeyCode)
        {
            //QuickTest
            case Keys.T:
                SetTestSaveValues();
                Editor.QuickTest();
                break;

            //Quick object delete
            case Keys.Delete:
                if (InspectorObject != null)
                {

                    Editor.RemoveObject(InspectorObject, Globals.SelectedArea, inspectorObjectScreen);
                    InspectorObject = null;
                    Room.SelectedObject = new Rectangle(-1, -1, 1, 1);
                    break;
                }
                Editor.RemoveObject(RoomSelectedCoordinate.X, RoomSelectedCoordinate.Y, Globals.SelectedArea);
                break;

            //TODO: some keybind to quickly edit scrolls

            //Copying tiles
            case Keys.C:
                if (e.Modifiers != Keys.Control) break;
                copyTiles();
                break;

            //Pasting tiles
            case Keys.V:
                if (e.Modifiers != Keys.Control) break;
                pasteTiles();
                break;

            //Zooming
            case Keys.Oemplus:
                toolbar_room.ZoomLevel++;
                Room.Zoom = toolbar_room.ZoomLevel;
                break;
            case Keys.OemMinus:
                toolbar_room.ZoomLevel--;
                Room.Zoom = toolbar_room.ZoomLevel;
                break;
        }
    }

    private void copyTiles()
    {
        //copy currently selected tiles
        TileSelection sel = new TileSelection(Editor.SelectionWidth, Editor.SelectionHeight, Editor.SelectedTiles);

        DataObject data = new DataObject();
        data.SetData(typeof(TileSelection), sel);
        Clipboard.SetDataObject(data, true);
    }

    private void pasteTiles()
    {
        DataObject retrievedData = Clipboard.GetDataObject() as DataObject;

        if (retrievedData == null || !retrievedData.GetDataPresent(typeof(TileSelection))) return;

        TileSelection sel = retrievedData.GetData(typeof(TileSelection)) as TileSelection;
        Editor.SelectedTiles = sel.Tiles;
        Editor.SelectionHeight = sel.SelectionHeight;
        Editor.SelectionWidth = sel.SelectionWidth;
        RoomSelectedSize.Height = sel.SelectionHeight * Room.PixelTileSize;
        RoomSelectedSize.Width = sel.SelectionWidth * Room.PixelTileSize;

        Rectangle rect = Room.RedRect; //old Position of the rectangle
        Room.RedRect = new Rectangle(RoomSelectedTile.X, RoomSelectedTile.Y, RoomSelectedSize.Width, RoomSelectedSize.Height);
        Rectangle unirect = Editor.UniteRect(Room.RedRect, rect);
        unirect.X -= 1;
        unirect.Y -= 1;
        unirect.Width += 2;
        unirect.Height += 2;
        Room.Invalidate(unirect);
    }

    private void btn_open_rom_Click(object sender, EventArgs e)
        => Editor.OpenProjectAndLoad();

    private void btn_new_project_Click(object sender, EventArgs e)
        => Editor.CreateNewProject();

    private void btn_save_project_Click(object sender, EventArgs e)
        => Editor.SaveProject();

    private void btn_tweaks_editor_Click(object sender, EventArgs e)
        => new TweaksEditor().Show();

    private void btn_open_rom_image_Click(object sender, EventArgs e)
        => btn_open_rom_Click(sender, e);

    private void btn_save_rom_image_Click(object sender, EventArgs e)
        => Editor.SaveProject();

    private void btn_create_backup_Click(object sender, EventArgs e)
        => Editor.CreateBackup();

    private void btn_data_viewer_Click(object sender, EventArgs e)
        => new DataViewer().Show();

    private void window_drag_over(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
            e.Effect = DragDropEffects.Link;
        else
            e.Effect = DragDropEffects.None;
    }

    private void window_file_drop(object sender, DragEventArgs e)
    {
        string[] s = (string[])e.Data.GetData(DataFormats.FileDrop, false);
        Editor.OpenProjectAndLoad(s[0]);
    }

    private void cbb_area_bank_SelectedIndexChanged(object sender, EventArgs e)
    {
        Globals.SelectedArea = cbb_area_bank.SelectedIndex;

        //resetting scroll bars
        flw_main_room_view.AutoScrollPosition = new Point(0, 0);
        flw_main_room_view.VerticalScroll.Value = 0;
        flw_main_room_view.HorizontalScroll.Value = 0;

        UpdateRoom();
    }

    private void btn_transition_editor_Click(object sender, EventArgs e)
        => new TransitionsEditor().Show();

    private void btn_open_transition_editor_image_Click(object sender, EventArgs e)
        => new TransitionsEditor().Show();

    private void btn_show_screen_outlines_Click(object sender, EventArgs e)
    {
        ToggleScreenOutlines();
        btn_show_screen_outlines.Checked = Room.ShowScreenOutlines;
    }

    private void btn_screen_settings_Click(object sender, EventArgs e)
    {
        new ScreenSettings(0, 0, Current).Show();
    }

    private void ctx_btn_screen_settings_Click(object sender, EventArgs e)
        => new ScreenSettings(Globals.SelectedArea, Globals.SelectedScreenNr, Current).Show();

    private void btn_show_duplicate_outlines_Click(object sender, EventArgs e)
    {
        ToggleDuplicateOutlines();
        btn_show_duplicate_outlines.Checked = Room.ShowDuplicateOutlines;
    }

    private void scrollBoundariesToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ToggleScrollBorders();
        btn_show_scrolls.Checked = btn_show_scroll_bounds.Checked = Room.ShowScrollBorders;
    }

    private void chb_view_objects_CheckedChanged(object sender, EventArgs e)
    {
        btn_view_show_objects.Checked = btn_show_objects.Checked = !btn_show_objects.Checked;
        Room.ShowObjects = btn_show_objects.Checked;

        for (int i = 0; i < 256; i++)
        {
            int screen = i + 256 * Globals.SelectedArea;
            if (Globals.Objects[screen].Count == 0) continue;

            foreach (Enemy o in Globals.Objects[screen])
            {
                Point p = o.GetPosition(screen % 256);
                Rectangle inv = new Rectangle(p.X, p.Y, 16, 16);
                Room.Invalidate(inv);
            }
        }
    }

    private void ctx_btn_test_here_Click(object sender, EventArgs e)
    {
        SetTestSaveValues();

        new TestRom(Globals.TestROMSave).Show();
    }

    private void ctx_btn_add_object_Click(object sender, EventArgs e)
    {
        Editor.AddObject(RoomSelectedTile.X, RoomSelectedTile.Y, Globals.SelectedArea);
    }

    private void btn_tileset_definitions_Click(object sender, EventArgs e)
    {
        new TilesetDefinitions().Show();
    }

    private void rOMFileToolStripMenuItem_Click(object sender, EventArgs e)
        => new ProgramSettings().ShowDialog();

    private void ctx_btn_remove_object_Click(object sender, EventArgs e)
    {
        Editor.RemoveObject(RoomSelectedCoordinate.X, RoomSelectedCoordinate.Y, Globals.SelectedArea);
        Room.SelectedObject = new Rectangle(-1, -1, 1, 1);
    }

    private void ctx_btn_edit_object_Click(object sender, EventArgs e)
        => new ObjectSettings(Editor.FindObject(RoomSelectedCoordinate.X, RoomSelectedCoordinate.Y, Globals.SelectedArea)).Show();

    private void btn_compile_ROM_Click(object sender, EventArgs e)
        => Editor.CompileROM();

    private void btn_project_settings_Click(object sender, EventArgs e)
        => new ProjectSettings().Show();

    private void btn_open_tileset_editor_Click(object sender, EventArgs e)
        => btn_tileset_definitions_Click(sender, e);

    private void tls_input_OnDataChanged(object sender, EventArgs e)
    {
        selectedTileset = tls_input.SelectedTileset;
        gfxOffset = tls_input.GraphicsOffset;
        MetatilePointer = tls_input.MetatilePointer;

        UpdateTileset();
        UpdateRoom();
    }

    private void btn_wiki_Click(object sender, EventArgs e)
    {
        string target = "https://github.com/ConConner/LAMP/wiki";
        Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
    }

    private void btn_save_editor_Click(object sender, EventArgs e)
    {
        new TestRom(Globals.InitialSaveGame, true).Show();
    }

    private void btn_bug_report_Click(object sender, EventArgs e)
    {
        string target = "https://github.com/ConConner/LAMP/issues";
        Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
    }

    private void btn_graphics_editor_Click(object sender, EventArgs e)
    {
        new GraphicsEditor().Show();
    }

    private void btn_converter_Click(object sender, EventArgs e)
    {
        Converter cv = new Converter();
        cv.TopLevel = true;
        cv.Show();
    }

    private void btn_show_converter_Click(object sender, EventArgs e)
    {
        btn_show_converter.Checked = !btn_show_converter.Checked;
        if (btn_show_converter.Checked) PermaConverter.Show();
        else PermaConverter.Hide();
    }

    private void MainWindow_Resize(object sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Maximized) Properties.programsettings.Default.startInFullscreen = true;
        else Properties.programsettings.Default.startInFullscreen = false;
        Properties.programsettings.Default.Save();
    }

    private void btn_solidity_editor_Click(object sender, EventArgs e)
        => new SolidityEditor().Show();

    private void btn_open_solidity_editor_Click(object sender, EventArgs e)
        => btn_solidity_editor_Click(sender, e);

    private void txb_obj_type_TextChanged(object sender, EventArgs e)
    {
        InspectorObject.Type = (byte)Format.StringToInt(txb_obj_type.Text, 0xFF);
    }

    private void txb_object_number_TextChanged(object sender, EventArgs e)
    {
        InspectorObject.Number = (byte)Format.StringToInt(txb_object_number.Text, 0xFF);
    }

    private void btn_auto_number_Click(object sender, EventArgs e)
    {
        for (int i = 0x40; i < 0x80; i++)
        {
            bool match = false;

            //loop through all screens
            for (int screen = 0; screen < 256; screen++)
            {
                //loop through every object per screen
                foreach (Enemy o in Globals.Objects[screen + 255 * Globals.SelectedArea])
                {
                    if (o == InspectorObject) continue;
                    if (o.Number == i)
                    {
                        match = true;
                        break;
                    }
                }
                if (match == true) break;
            }
            if (match == true) continue;

            //If code gets to this point, there is no object sharing the number
            txb_object_number.Text = Format.IntToString(i);
            return;
        }
        MessageBox.Show("There is no unusedn, non respawning Number left in this Bank!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void btn_open_collision_editor_Click(object sender, EventArgs e)
        => new CollisionEditor(0).Show();

    private void btn_open_tweaks_editor_image_Click(object sender, EventArgs e) => new DataChunkEditor().Show();

    private void btn_edit_tileset_Click(object sender, EventArgs e)
    {
        if (Globals.LoadedProject.useTilesets) new GraphicsEditor(selectedTileset).Show();
        else new GraphicsEditor(gfxOffset, MetatilePointer).Show();
    }

    private void Btn_Area_Clear_Click(object sender, EventArgs e)
    {
        if (MessageBox.Show("Do you want to erase all tiles in this area?\n" +
            "(this action cannot be undone)",
            "Clear All Tiles",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning) == DialogResult.No) { return; }

        //Clearing all area screens
        List<GameScreen> screenList = Globals.Screens[cbb_area_bank.SelectedIndex];

        foreach (GameScreen screen in screenList)
        {
            for (int i = 0; i < screen.Data.Length; i++)
            {
                screen.Data[i] = Globals.LoadedProject.FillTile;
            }
        }

        //Redraw area
        Editor.DrawAreaBank(cbb_area_bank.SelectedIndex, Globals.AreaBank, new Point(0, 0));
        Room.BackgroundImage = Globals.AreaBank;
        Room.Invalidate();
    }

    private void Btn_Area_Replace_Click(object sender, EventArgs e)
    {
        //Replacing all instances in an area of a selected tile with another tile
        List<GameScreen> screenList = Globals.Screens[cbb_area_bank.SelectedIndex];
        byte targetTile = selectedTileId;
        TileSelectDialog dialog = new TileSelectDialog(selectedTileset);

        if (dialog.ShowDialog() == DialogResult.Cancel) { return; }

        byte newTile = (byte)dialog.Result;

        foreach (GameScreen screen in screenList)
        {
            for (int i = 0; i < screen.Data.Length; i++)
            {
                byte curTile = screen.Data[i];
                if (curTile == targetTile) screen.Data[i] = newTile;
            }
        }

        //Redraw area
        Editor.DrawAreaBank(cbb_area_bank.SelectedIndex, Globals.AreaBank, new Point(0, 0));
        Room.BackgroundImage = Globals.AreaBank;
        Room.Invalidate();
    }
    #endregion

    #endregion

    private void BtnTest_Click(object sender, EventArgs e)
    {
        Editor.ConvertSymbolToPointer("E:\\METROID\\Metroid 2 RoS\\M2RoS\\out\\M2RoS.sym");
    }
}