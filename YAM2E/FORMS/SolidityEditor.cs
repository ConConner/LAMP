using LAMP.Classes;
using LAMP.Classes.M2_Data;
using LAMP.Utilities;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LAMP.FORMS;

public partial class SolidityEditor : Form
{
    Pointer gfxOverlay = null;
    Pointer tableStart = new Pointer(0x8, 0x7EFA);
    Pointer currentOffset = null;
    int selectedIndex = 0;

    public SolidityEditor(int index = 0, Pointer gfxOffset = null)
    {
        InitializeComponent();
        gfxOverlay = gfxOffset;
        if (gfxOffset != null) txb_offset.Text = Format.PointerToString(gfxOffset);
        cbb_solidity_table.SelectedIndex = index;

        //Toolbar
        toolbar.SetTools(false, false, false, false);
        toolbar.SetTransform(false, false, false, false);
        toolbar.SetCopyPaste(false, false);
        toolbar.DisableZoomSeperator();
    }

    private void cbb_solidity_table_SelectedIndexChanged(object sender, EventArgs e)
    {
        currentOffset = tableStart + 4 * cbb_solidity_table.SelectedIndex;
        txb_samus.Text = Format.IntToString(Editor.ROM.Read8(currentOffset));
        txb_objects.Text = Format.IntToString(Editor.ROM.Read8(currentOffset + 1));
        txb_projectiles.Text = Format.IntToString(Editor.ROM.Read8(currentOffset + 2));
    }

    private void btn_apply_Click(object sender, EventArgs e)
    {
        byte samus = (byte)Format.StringToInt(txb_samus.Text, 0x7F);
        byte objects = (byte)Format.StringToInt(txb_objects.Text, 0x7F);
        byte projectiles = (byte)Format.StringToInt(txb_projectiles.Text, 0x7F);

        //Saving the data
        DataChunk dc = new DataChunk(currentOffset, new byte[] { samus, objects, projectiles, 0xFF }, "Solidity");
        Editor.AddDataChunk(dc);

        //Changing txb text
        txb_samus.Text = Format.IntToString(samus);
        txb_objects.Text = Format.IntToString(objects);
        txb_projectiles.Text = Format.IntToString(projectiles);
    }

    private void txb_offset_TextChanged(object sender, EventArgs e)
    {
        gfxOverlay = Format.StringToPointer(txb_offset.Text);
        if (gfxOverlay == null || txb_offset.Text == "")
        {
            Tileset.BackgroundImage = null;
            toolbar.Enabled = false;
            Tileset.ResetSelection();
            return;
        }
        toolbar.Enabled = true;
        GFX TilesetGFX = new GFX(gfxOverlay, 16, 8);
        TilesetGFX.Draw();
        Tileset.BackgroundImage = TilesetGFX.Image;
    }

    private void toolbar_ToolCommandTriggered(object sender, EventArgs e)
    {
        switch (toolbar.TriggeredCommand)
        {
            case LampToolCommand.ZoomIn:
            case LampToolCommand.ZoomOut:
                Tileset.Zoom = toolbar.ZoomLevel;
                break;
        }
    }

    private void Tileset_MouseMove(object sender, MouseEventArgs e)
    {
        if (Tileset.BackgroundImage == null) return;

        //The currently selected pixel
        Point pixel = new Point(Math.Max(Math.Min(e.X, Tileset.BackgroundImage.Width * Tileset.Zoom - 1), 0) / Tileset.Zoom, Math.Max(Math.Min(e.Y, Tileset.BackgroundImage.Height * Tileset.Zoom - 1), 0) / Tileset.Zoom);
        Point tileNum = new Point(pixel.X / Tileset.PixelTileSize, pixel.Y / Tileset.PixelTileSize); //The number of the tile selected
        Point tile = new Point(tileNum.X * Tileset.PixelTileSize, tileNum.Y * Tileset.PixelTileSize); //The room coordinates of the selected tile

        Point selStart = new Point(tileNum.X * Tileset.TileSize, tileNum.Y * Tileset.TileSize);
        Tileset.RedRect = new Rectangle(selStart.X, selStart.Y, Tileset.TileSize - 1, Tileset.TileSize - 1);
        selectedIndex = tileNum.Y * 16 + tileNum.X;
        lbl_tile_id.Text = $"Selected Index: {Format.IntToString(selectedIndex)}";
    }

    private void btn_set_samus_Click(object sender, EventArgs e) => txb_samus.Text = Format.IntToString(selectedIndex + 1);

    private void btn_set_object_Click(object sender, EventArgs e) => txb_objects.Text = Format.IntToString(selectedIndex + 1);

    private void btn_set_projectiles_Click(object sender, EventArgs e) => txb_projectiles.Text = Format.IntToString(selectedIndex + 1);

    private void btn_set_all_Click(object sender, EventArgs e)
    {
        txb_samus.Text = Format.IntToString(selectedIndex + 1);
        txb_objects.Text = Format.IntToString(selectedIndex + 1);
        txb_projectiles.Text = Format.IntToString(selectedIndex + 1);
    }

    private void btn_import_Click(object sender, EventArgs e)
    {
        string path = Editor.ShowOpenDialog("Binary File (*.*)|*.*");
        if (!File.Exists(path)) return;

        //Reading data from file
        byte[] data = File.ReadAllBytes(path);

        //Checking if file is valid
        //Size
        if (data.Length > 4)
        {
            MessageBox.Show("Data is too long", "Error", MessageBoxButtons.OK);
            return;
        }
        if (data.Length < 4)
        {
            MessageBox.Show("Data is too short", "Error", MessageBoxButtons.OK);
            return;
        }


        Editor.AddDataChunk(new DataChunk(currentOffset, data, "Solidity"));
        cbb_solidity_table_SelectedIndexChanged(null, null);
    }

    private void btn_export_Click(object sender, EventArgs e)
    {
        string path = Editor.ShowSaveDialog("Binary File (*.*)|*.*");
        if (path == string.Empty) return;

        byte samus = (byte)Format.StringToInt(txb_samus.Text, 0x7F);
        byte objects = (byte)Format.StringToInt(txb_objects.Text, 0x7F);
        byte projectiles = (byte)Format.StringToInt(txb_projectiles.Text, 0x7F);

        File.WriteAllBytes(path, new byte[] {samus, objects, projectiles, 0xFF});
    }
}
