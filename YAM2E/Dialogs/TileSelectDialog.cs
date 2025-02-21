using LAMP.Classes;
using LAMP.Classes.M2_Data;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace LAMP.FORMS
{
    public partial class TileSelectDialog : Form
    {
        public TileSelectDialog(Tileset tileSet)
        {
            InitializeComponent();
            Construct(tileSet.GfxOffset, Editor.GetMetaPointerFromTable(tileSet.MetatileTable));
        }

        public TileSelectDialog(Pointer gfxPointer, Pointer metatilePointer)
        {
            InitializeComponent();
            Construct(gfxPointer, metatilePointer);
        }

        private void Construct(Pointer gfxPointer, Pointer metatilePointer)
        {
            tool_bar.SetTools(false, false, true, false);
            tool_bar.SetCopyPaste(false, false);
            tool_bar.SetTransform(false, false, false, false);

            gfx = new GFX(gfxPointer, 16, 8);
            metatiles = new Metatiles(gfx, metatilePointer);

            tile_viewer.BackgroundImage = metatiles.Draw();
        }

        GFX gfx;
        Metatiles metatiles;
        public int Result;

        private void tool_bar_ToolCommandTriggered(object sender, EventArgs e)
        {
            switch (tool_bar.TriggeredCommand)
            {
                case (LampToolCommand.ZoomIn):
                case (LampToolCommand.ZoomOut):
                    tile_viewer.Zoom = tool_bar.ZoomLevel;
                    break;
            }
        }
        private void tile_viewer_MouseDown(object sender, MouseEventArgs e)
        {
            //The currently selected pixel
            Point pixel = new Point(Math.Max(Math.Min(e.X, tile_viewer.BackgroundImage.Width * tile_viewer.Zoom - 1), 0) / tile_viewer.Zoom, Math.Max(Math.Min(e.Y, tile_viewer.BackgroundImage.Height * tile_viewer.Zoom - 1), 0) / tile_viewer.Zoom);
            Point tileNum = new Point(pixel.X / tile_viewer.PixelTileSize, pixel.Y / tile_viewer.PixelTileSize); //The number of the tile selected
            Point tile = new Point(tileNum.X * tile_viewer.PixelTileSize, tileNum.Y * tile_viewer.PixelTileSize); //The room coordinates of the selected tile

            Result = tileNum.Y * 16 + tileNum.X;
            btn_replace.Enabled = true;

            tile_viewer.SelRect = tile_viewer.RedRect;
        }

        private void tile_viewer_MouseMove(object sender, MouseEventArgs e)
        {
            //The currently selected pixel
            Point pixel = new Point(Math.Max(Math.Min(e.X, tile_viewer.BackgroundImage.Width * tile_viewer.Zoom - 1), 0) / tile_viewer.Zoom, Math.Max(Math.Min(e.Y, tile_viewer.BackgroundImage.Height * tile_viewer.Zoom - 1), 0) / tile_viewer.Zoom);
            Point tileNum = new Point(pixel.X / tile_viewer.PixelTileSize, pixel.Y / tile_viewer.PixelTileSize); //The number of the tile selected
            Point tile = new Point(tileNum.X * tile_viewer.PixelTileSize, tileNum.Y * tile_viewer.PixelTileSize); //The room coordinates of the selected tile

            Point selStart = new Point(tileNum.X * tile_viewer.TileSize, tileNum.Y * tile_viewer.TileSize);
            tile_viewer.RedRect = new Rectangle(selStart.X, selStart.Y, tile_viewer.TileSize - 1, tile_viewer.TileSize - 1);
        }

        private void btn_replace_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Do you want to replace all tiles in this area?\n" +
                "(this action cannot be undone)",
                "Replace All Tiles",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) == DialogResult.No) DialogResult = DialogResult.Cancel;
        }
    }
}
