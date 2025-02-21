using System.Collections.Generic;
using System.Drawing;
using LAMP.Classes;
using LAMP.Classes.M2_Data;
using LAMP.Controls;
using LAMP.Interfaces;

namespace LAMP.Actions;

internal class PlaceTileAction : IAction
{
    public PlaceTileAction(Point location, byte[] tiles, int width, int height, RoomViewer viewer, bool generateUndo = true)
    {
        TargetWidth = width;
        TargetHeight = height;
        TargetLocation = location;
        TargetTiles = tiles;
        TargetRoomViewer = viewer;

        replaceTiles = GenerateReplaceTiles(location, width, height);

        if (generateUndo) //Save the Tile IDs of the tiles that get replaced for later use.
        {
            OldTiles = new byte[TargetTiles.Length];
            for (int i = 0; i < replaceTiles.Length; i++) OldTiles[i] = replaceTiles[i].TileID;
        }
    }

    //FIELDS
    public RoomViewer TargetRoomViewer { get; set; }
    public Point TargetLocation { get; set; }
    public int TargetWidth { get; set; }
    public int TargetHeight { get; set; }
    private RoomTile[] replaceTiles { get; set; }
    private byte[] OldTiles { get; set; }
    public byte[] TargetTiles { get; set; }

    public string Description => "Placed Tile";

    //Methods
    /// <summary>
    /// Generates an Array that contains the tiles that have to be replaced
    /// </summary>
    /// <returns></returns>
    private RoomTile[] GenerateReplaceTiles(Point location, int width, int height)
    {
        RoomTile[] replaceTiles = new RoomTile[width * height];
        int count = 0;
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                int tx = location.X + 16 * j;
                int ty = location.Y + 16 * i;
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

        return replaceTiles;
    }


    //Interface
    /// <summary>
    /// Places down all the tiles saved in <see cref="TargetTiles"/> and updates the screen visuals
    /// </summary>
    public void Do()
    {
        //Writing tile Data to screen data
        int count = 0;
        List<int> updatedScreens = new();
        foreach (RoomTile t in replaceTiles)
        {
            if (t.Unused) continue;
            t.ReplaceTile(TargetTiles[count]);
            if (!updatedScreens.Contains(t.ScreenNr)) updatedScreens.Add(t.ScreenNr);
            Editor.DrawScreen(Globals.SelectedArea, t.ScreenNr);
            count++;
        }

        //Rewriting the updated screens
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
            TargetRoomViewer.Invalidate(new Rectangle(sx * TargetRoomViewer.Zoom, sy * TargetRoomViewer.Zoom, 256 * TargetRoomViewer.Zoom, 256 * TargetRoomViewer.Zoom));
            count++;
        }
        g.Dispose();
    }

    public void Undo()
    {
        if (OldTiles == null) throw new System.Exception("Undo action not set up correctly. No old tiles known!");

        //To undo this action, we are just going to call the Do function of a new Action with the Old tiles as Input
        new PlaceTileAction(TargetLocation, OldTiles, TargetWidth, TargetHeight, TargetRoomViewer, false).Do();
    }
}
