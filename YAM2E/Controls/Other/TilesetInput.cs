using LAMP.Classes;
using LAMP.Utilities;
using System;
using System.Windows.Forms;

namespace LAMP.Controls.Other;

public partial class TilesetInput : UserControl
{
    public TilesetInput()
    {
        InitializeComponent();
        setMode();
    }

    //Fields
    //Data
    public Pointer MetatilePointer { get; set; }
    public Pointer GraphicsOffset { get; set; }
    public Tileset SelectedTileset { get; set; } = null;
    public int MetatileTable { get; set; }
    public bool ForceOffsets { get; set; } = false;

    #region EVENT
    //Events
    public EventHandler onDataChanged { get; set; }
    public event EventHandler DataChanged
    {
        add
        {
            onDataChanged += value;
        }
        remove
        {
            onDataChanged -= value;
        }

    }
    public virtual void OnDataChanged(EventArgs e)
    {
        onDataChanged?.Invoke(this, e);
    }
    #endregion

    /// <summary>
    /// Sets the offset mode or tileset mode according to the users setting
    /// </summary>
    public void setMode()
    {
        if (Globals.LoadedProject == null || Globals.LoadedProject.useTilesets != true || Globals.Tilesets.Count < 1 || ForceOffsets)
        {
            pnl_tilesets.Visible = false;
            pnl_offsets.Visible = true;
        }
        else
        {
            pnl_offsets.Visible = false;
            pnl_tilesets.Visible = true;
            populateTilesets();
        }
    }

    /// <summary>
    /// Sets the graphics
    /// </summary>
    public void SetGraphics(Pointer gfx, int metatable)
    {
        if (gfx != null) txb_graphics_offset.Text = Format.PointerToString(gfx);
        cbb_metatile_table.SelectedIndex = metatable;
        MetatileTable = metatable;

        GraphicsOffset = gfx;
        MetatilePointer = Editor.GetMetaPointerFromTable(metatable);

        OnDataChanged(new EventArgs());
    }

    /// <summary>
    /// Sets the selected Tileset
    /// </summary>
    public void SetTileset(int TilesetIndex)
    {
        cbb_tileset_id.SelectedIndex = TilesetIndex;
    }

    /// <summary>
    /// Loads all available tilese
    /// </summary>
    public void populateTilesets()
    {
        //saving current selected entry
        int selected = Math.Max(cbb_tileset_id.SelectedIndex, 0);

        //Adding entries
        cbb_tileset_id.Items.Clear();
        for (int i = 0; i < Globals.Tilesets.Count; i++)
        {
            Tileset t = Globals.Tilesets[i];

            //generating name
            string name = i.ToString();
            if (t.Name != "") name = t.Name;

            //adding to selection
            cbb_tileset_id.Items.Add(name);
        }
        cbb_tileset_id.AutoSize();
        cbb_tileset_id.SelectedIndex = Math.Min(cbb_tileset_id.Items.Count - 1, selected); //Setting last selected tileset
    }


    private void cbb_tileset_id_SelectedIndexChanged(object sender, EventArgs e)
    {
        SelectedTileset = Globals.Tilesets[cbb_tileset_id.SelectedIndex];

        OnDataChanged(new EventArgs());
    }

    private void txb_graphics_offset_TextChanged(object sender, EventArgs e)
    {
        Pointer p = Format.StringToPointer(txb_graphics_offset.Text);
        if (p != null && p.Offset < Editor.ROM.Size)
        {
            GraphicsOffset = p;
            MetatilePointer = Editor.GetMetaPointerFromTable(cbb_metatile_table.SelectedIndex);
        }

        OnDataChanged(new EventArgs());
    }

    private void txb_graphics_offset_Leave(object sender, EventArgs e)
    {
        txb_graphics_offset.Text = Format.PointerToString(GraphicsOffset);
    }

    private void cbb_metatile_table_SelectedIndexChanged(object sender, EventArgs e)
    {
        SetGraphics(GraphicsOffset, cbb_metatile_table.SelectedIndex);
    }
}
