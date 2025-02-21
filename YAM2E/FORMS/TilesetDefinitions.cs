using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using LAMP.Classes;
using LAMP.Controls;
using LAMP.Utilities;
using LAMP.Classes.M2_Data;
using System.Text.Json;

namespace LAMP.FORMS;

public partial class TilesetDefinitions : Form
{
    public TileViewer Tileset = new TileViewer()
    {
        Location = new Point(3, 3),
        BackColor = Globals.ColorBlack,
        Cursor = Cursors.Hand,
    };
    private Pointer MetatilePointer;

    public TilesetDefinitions()
    {
        InitializeComponent();

        //setting indices
        cbb_metatile_table.SelectedIndex = 0;
        cbb_collision_table.SelectedIndex = 0;
        cbb_solidity_table.SelectedIndex = 0;

        if (Globals.Tilesets.Count < 1) DisableComponents();
        else
        {
            UpdateIdList();
        }

        //Toolbar
        Toolbar.SetTools(false, false, false, false);
        Toolbar.SetTransform(false, false, false, false);
        Toolbar.SetCopyPaste(false, false);
        Toolbar.DisableZoomSeperator();

        //Adding Preview Tileset
        pnl_preview.Controls.Add(Tileset);
        Tileset.BringToFront();
        Tileset.ResetSelection();
        Tileset.Click += btn_edit_Click;
        UpdateTileset();
    }

    private void UpdateTileset()
    {
        if (Globals.Tilesets.Count < 1) return;
        Tileset.BackgroundImage?.Dispose();

        GFX gfx = new GFX(Globals.Tilesets[cbb_tileset_id.SelectedIndex].GfxOffset, 16, 8);
        Metatiles meta = new Metatiles(gfx, MetatilePointer);

        Tileset.BackgroundImage = meta.Draw();
        //grp_tileset_preview.Size = new Size(Tileset.BackgroundImage.Width + 30, Tileset.BackgroundImage.Height + 35);
    }

    private void cbb_metatile_table_SelectedIndexChanged(object sender, EventArgs e)
    {
        //Table to offset
        MetatilePointer = new Pointer(0x8, Editor.ROM.Read16(Editor.ROM.MetatilePointers.Offset + 2 * cbb_metatile_table.SelectedIndex));
    }

    private void EnableComponents()
    {
        grp_tileset_preview.Visible = true;
        txb_gfx_offset.Enabled = true;
        cbb_metatile_table.Enabled = true;
        cbb_collision_table.Enabled = true;
        cbb_solidity_table.Enabled = true;
        txb_tileset_name.Enabled = true;
        lbl_metatile_table.Enabled = true;
        lbl_collision_table.Enabled = true;
        lbl_soliditiy_table.Enabled = true;
        lbl_tileset_name.Enabled = true;
        btn_save_tileset.Enabled = true;
        btn_remove_tileset.Enabled = true;
        lbl_tileset_gfx_offset.Enabled = true;
        cbb_tileset_id.Enabled = true;
        lbl_tileset_id.Enabled = true;
        btn_edit.Enabled = true;
        btn_edit_collision.Enabled = true;
        btn_edit_solidity.Enabled = true;
        Toolbar.Enabled = true;
    }
    private void DisableComponents()
    {
        grp_tileset_preview.Visible = false;
        txb_gfx_offset.Enabled = false;
        cbb_metatile_table.Enabled = false;
        cbb_collision_table.Enabled = false;
        cbb_solidity_table.Enabled = false;
        txb_tileset_name.Enabled = false;
        lbl_metatile_table.Enabled = false;
        lbl_collision_table.Enabled = false;
        lbl_soliditiy_table.Enabled = false;
        lbl_tileset_name.Enabled = false;
        btn_save_tileset.Enabled = false;
        btn_remove_tileset.Enabled = false;
        lbl_tileset_gfx_offset.Enabled = false;
        cbb_tileset_id.Enabled = false;
        lbl_tileset_id.Enabled = false;
        btn_edit.Enabled = false;
        btn_edit_collision.Enabled = false;
        btn_edit_solidity.Enabled = false;
        Toolbar.Enabled = false;

        cbb_tileset_id.Text = "";
        cbb_metatile_table.SelectedIndex = 0;
        cbb_collision_table.SelectedIndex = 0;
        cbb_solidity_table.SelectedIndex = 0;
        txb_gfx_offset.Text = "";
        txb_tileset_name.Text = "";
    }

    private void btn_add_tileset_Click(object sender, EventArgs e)
    {
        EnableComponents();
        Globals.Tilesets.Add(new Tileset());
        UpdateIdList();
        MainWindow.Current.SwitchTilesetOffsetMode();
        MainWindow.Current.LoadTilesetList();
    }

    private void UpdateIdList()
    {
        if (Globals.Tilesets.Count == 0)
        {
            DisableComponents();
            return;
        }

        //Updating the combobox with all the tilesets
        cbb_tileset_id.Items.Clear();
        for (int i = 0; i < Globals.Tilesets.Count; i++)
        {
            Tileset t = Globals.Tilesets[i];
            string name = i.ToString("X");
            if (t.Name != "") name = t.Name;
            cbb_tileset_id.Items.Add(name);
        }
        cbb_tileset_id.SelectedIndex = Globals.Tilesets.Count - 1;
        if (Globals.Tilesets.Count > 0) btn_save_tileset.Enabled = true;
        cbb_tileset_id.AutoSize();
    }

    private void UpdateNames()
    {
        //Updating the names of the list
        for (int i = 0; i < Globals.Tilesets.Count; i++)
        {
            Tileset t = Globals.Tilesets[i];
            string name = i.ToString("X");
            if (t.Name != "") name = t.Name;

            cbb_tileset_id.Items[i] = name;
        }
        cbb_tileset_id.AutoSize();
    }

    private void btn_save_tileset_Click(object sender, EventArgs e)
    {
        //saving tileset object and tileset list
        //object
        Tileset t = Globals.Tilesets[cbb_tileset_id.SelectedIndex];
        t.Name = txb_tileset_name.Text;

        Pointer gfxOffset = Format.StringToPointer(txb_gfx_offset.Text);
        t.GfxOffset = gfxOffset == null ? new Pointer(0) : gfxOffset;
        txb_gfx_offset.Text = Format.PointerToString(t.GfxOffset);

        t.MetatileTable = cbb_metatile_table.SelectedIndex;
        t.CollisionTable = cbb_collision_table.SelectedIndex;
        t.SolidityTable = cbb_solidity_table.SelectedIndex;

        //Updating preview
        UpdateTileset();
        UpdateNames();

        //Updating main window list
        MainWindow.Current.SwitchTilesetOffsetMode();
        MainWindow.Current.LoadTilesetList();
    }

    private void cbb_tileset_id_SelectedIndexChanged(object sender, EventArgs e)
    {
        Tileset t = Globals.Tilesets[cbb_tileset_id.SelectedIndex];
        txb_gfx_offset.Text = Format.PointerToString(t.GfxOffset);
        txb_tileset_name.Text = t.Name;
        cbb_metatile_table.SelectedIndex = t.MetatileTable;
        cbb_collision_table.SelectedIndex = t.CollisionTable;
        cbb_solidity_table.SelectedIndex = t.SolidityTable;

        UpdateTileset();
    }

    private void btn_remove_tileset_Click(object sender, EventArgs e)
    {
        Globals.Tilesets.RemoveAt(cbb_tileset_id.SelectedIndex);
        UpdateIdList();
        MainWindow.Current.SwitchTilesetOffsetMode();
        MainWindow.Current.LoadTilesetList();
    }

    private void btn_edit_Click(object sender, EventArgs e)
    {
        new GraphicsEditor(Globals.Tilesets[cbb_tileset_id.SelectedIndex]).Show();
    }

    private void btn_edit_solidity_Click(object sender, EventArgs e)
    {
        new SolidityEditor(cbb_solidity_table.SelectedIndex, Format.StringToPointer(txb_gfx_offset.Text)).Show();
    }

    private void btn_edit_collision_Click(object sender, EventArgs e)
    {
        new CollisionEditor(cbb_collision_table.SelectedIndex, Format.StringToPointer(txb_gfx_offset.Text)).Show();
    }

    private void Toolbar_ToolCommandTriggered(object sender, EventArgs e)
    {
        switch (Toolbar.TriggeredCommand)
        {
            case (LampToolCommand.ZoomIn):
            case (LampToolCommand.ZoomOut):
                Tileset.Zoom = Toolbar.ZoomLevel;
                break;
        }
    }

    private void btn_import_Click(object sender, EventArgs e)
    {
        string path = Editor.ShowOpenDialog("LAMP Tileset (*ltf)|*.ltf");
        if (!File.Exists(path)) return;

        btn_save_tileset_Click(null, null);

        Tileset t = Globals.Tilesets[cbb_tileset_id.SelectedIndex];
        ExportTileset r = JsonSerializer.Deserialize<ExportTileset>(File.ReadAllText(path));

        //Writing data chunks for everything
        txb_tileset_name.Text = r.Name;
        Editor.AddDataChunk(new DataChunk(t.GfxOffset, r.Graphics));
        Editor.AddDataChunk(new DataChunk(Editor.GetMetaPointerFromTable(t.MetatileTable), r.MetatileTable));
        Editor.AddDataChunk(new DataChunk(Editor.GetCollisionPointerFromTable(t.CollisionTable), r.CollisionTable));
        Editor.AddDataChunk(new DataChunk(Editor.GetSolidityPointerFromTable(t.SolidityTable), r.SolidityTable));

        btn_save_tileset_Click(null, null); //Saving again to update the preview and to update the name
    }

    private void btn_export_Click(object sender, EventArgs e)
    {
        string path = Editor.ShowSaveDialog("LAMP Tileset (*.ltf)|*.ltf");
        if (path == string.Empty) return;

        btn_save_tileset_Click(null, null);

        Tileset t = Globals.Tilesets[cbb_tileset_id.SelectedIndex];
        ExportTileset r = new ExportTileset();

        //Reading all the Data
        r.Name = t.Name;
        Editor.ROM.ReadBytes(t.GfxOffset, r.Graphics, 2048);
        Editor.ROM.ReadBytes(Editor.GetMetaPointerFromTable(t.MetatileTable), r.MetatileTable, 512);
        Editor.ROM.ReadBytes(Editor.GetCollisionPointerFromTable(t.CollisionTable), r.CollisionTable, 256);
        Editor.ROM.ReadBytes(Editor.GetSolidityPointerFromTable(t.SolidityTable), r.CollisionTable, 4);

        //Saving the data
        Editor.SaveJsonObject(r, path);
    }
}
