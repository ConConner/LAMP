using LAMP.Classes;
using LAMP.Classes.M2_Data;
using LAMP.Utilities;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LAMP.FORMS
{
    public partial class CollisionEditor : Form
    {
        Pointer GfxOverlay = null;
        Pointer TableStart = new Pointer(0x8, 0x7EEA);
        Pointer currentOffset = null;
        bool updatingChecks;
        byte[] Data = new byte[256];

        public CollisionEditor(int index = 1, Pointer gfxOffset = null)
        {
            InitializeComponent();
            GfxOverlay = gfxOffset;
            if (gfxOffset != null) txb_offset.Text = Format.PointerToString(gfxOffset);
            cbb_collision_table.SelectedIndex = index;

            toolbar.SetCopyPaste(false, false);
            toolbar.SetTransform(false, false, false, false);
            toolbar.SetTools(false, false, true, false);
            toolbar.SelectedTool = LampTool.Select;
        }

        private int CurrentTileIndex
        {
            get => currentTileIndex;
            set
            {
                if (Tileset.BackgroundImage == null)
                {
                    grp_collision.Visible = false;
                    return;
                }
                grp_collision.Visible = true;
                currentTileIndex = value;

                updatingChecks = true;

                byte b = Data[value];
                chb_water.Checked = ByteOp.IsBitSet(b, 0);
                chb_pass_through_bottom.Checked = ByteOp.IsBitSet(b, 1);
                chb_pass_through_ceiling.Checked = ByteOp.IsBitSet(b, 2);
                chb_spike.Checked = ByteOp.IsBitSet(b, 3);
                chb_acid.Checked = ByteOp.IsBitSet(b, 4);
                chb_shot_block.Checked = ByteOp.IsBitSet(b, 5);
                chb_bomb_block.Checked = ByteOp.IsBitSet(b, 6);
                chb_save.Checked = ByteOp.IsBitSet(b, 7);

                updatingChecks = false;
            }
        }
        private int currentTileIndex = 0;

        private void txb_offset_TextChanged(object sender, EventArgs e)
        {
            GfxOverlay = Format.StringToPointer(txb_offset.Text);
            if (GfxOverlay == null || txb_offset.Text == "")
            {
                Tileset.BackgroundImage = null;
                toolbar.Enabled = false;
                Tileset.ResetSelection();
                grp_collision.Visible = false;
                return;
            }
            toolbar.Enabled = true;
            GFX TilesetGFX = new GFX(GfxOverlay, 16, 8);
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

        private void cbb_collision_table_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Read the selected pointer in the pointer table
            currentOffset = new Pointer(0x8, Editor.ROM.Read16(TableStart + 2 * cbb_collision_table.SelectedIndex));

            Editor.ROM.ReadBytes(currentOffset, Data, 256); //Read the entire table
            CurrentTileIndex = currentTileIndex; //This should reset the checkboxes
        }

        private void Tileset_MouseDown(object sender, MouseEventArgs e)
        {
            if (Tileset.BackgroundImage == null) return;

            //The currently selected pixel
            Point pixel = new Point(Math.Max(Math.Min(e.X, Tileset.BackgroundImage.Width * Tileset.Zoom - 1), 0) / Tileset.Zoom, Math.Max(Math.Min(e.Y, Tileset.BackgroundImage.Height * Tileset.Zoom - 1), 0) / Tileset.Zoom);
            Point tileNum = new Point(pixel.X / Tileset.PixelTileSize, pixel.Y / Tileset.PixelTileSize); //The number of the tile selected
            Point tile = new Point(tileNum.X * Tileset.PixelTileSize, tileNum.Y * Tileset.PixelTileSize); //The room coordinates of the selected tile

            CurrentTileIndex = tileNum.Y * 16 + tileNum.X;
            Tileset.SelRect = Tileset.RedRect;
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
        }

        private void chb_water_CheckedChanged(object sender, EventArgs e)
        {
            if (updatingChecks) return;

            byte b = 0;
            if (chb_water.Checked) b = ByteOp.SetBit(b, 0, 1);
            if (chb_pass_through_bottom.Checked) b = ByteOp.SetBit(b, 1, 1);
            if (chb_pass_through_ceiling.Checked) b = ByteOp.SetBit(b, 2, 1);
            if (chb_spike.Checked) b = ByteOp.SetBit(b, 3, 1);
            if (chb_acid.Checked) b = ByteOp.SetBit(b, 4, 1);
            if (chb_shot_block.Checked) b = ByteOp.SetBit(b, 5, 1);
            if (chb_bomb_block.Checked) b = ByteOp.SetBit(b, 6, 1);
            if (chb_save.Checked) b = ByteOp.SetBit(b, 7, 1);

            Data[currentTileIndex] = b;
        }

        private void btn_apply_Click(object sender, EventArgs e)
        {
            Editor.AddDataChunk(new DataChunk(currentOffset, Data, "CollisionTable"));
        }

        private void btn_import_Click(object sender, EventArgs e)
        {
            string path = Editor.ShowOpenDialog("Binary File (*.*)|*.*");
            if (!File.Exists(path)) return;

            //Reading data from file
            byte[] data = File.ReadAllBytes(path);

            //Checking if file is valid
            //Size
            if (data.Length > 256)
            {
                MessageBox.Show("Data is too long", "Error", MessageBoxButtons.OK);
                return;
            }
            if (data.Length < 256)
            {
                MessageBox.Show("Data is too short", "Error", MessageBoxButtons.OK);
                return;
            }

            Editor.AddDataChunk(new DataChunk(currentOffset, data, "CollisionTable"));
            cbb_collision_table_SelectedIndexChanged(null, null);
        }

        private void btn_export_Click(object sender, EventArgs e)
        {
            string path = Editor.ShowSaveDialog("Binary File (*.*)|*.*");
            if (path == string.Empty) return;

            File.WriteAllBytes(path, Data);
        }
    }
}
