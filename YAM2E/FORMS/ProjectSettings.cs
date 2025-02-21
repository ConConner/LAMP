using LAMP.Classes;
using LAMP.Utilities;
using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace LAMP.FORMS;

public partial class ProjectSettings : Form
{
    bool construct;

    public ProjectSettings()
    {
        construct = true;

        InitializeComponent();
        chb_rmv_mt_o_list.Checked = Globals.LoadedProject.OptimizeObjectData;
        chb_fix_object_loading.Checked = Globals.LoadedProject.FixVerticalObjectLoading;
        rbt_use_tilesets.Checked = Globals.LoadedProject.useTilesets;
        txb_rom_path.Text = Globals.LoadedProject.ProjectSpecificROM;
        txb_disassembly_path.Text = Globals.LoadedProject.DisassemblyPath;
        txb_default_tile.Text = Format.IntToString(Globals.LoadedProject.FillTile);
        chb_assemble_compile.Checked = Globals.LoadedProject.BuildAssemblyWhenCompiling;

        construct = false;
    }

    private void chb_rmv_mt_o_list_CheckedChanged(object sender, EventArgs e)
    {
        Globals.LoadedProject.OptimizeObjectData = chb_rmv_mt_o_list.Checked;
    }

    private void rbt_use_offsets_CheckedChanged(object sender, EventArgs e)
    {
        if (rbt_use_offsets.Checked)
        {
            Globals.LoadedProject.useTilesets = false;
            MainWindow.Current.SwitchTilesetOffsetMode();
        }
    }

    private void rbt_use_tilesets_CheckedChanged(object sender, EventArgs e)
    {
        if (rbt_use_tilesets.Checked)
        {
            Globals.LoadedProject.useTilesets = true;
            MainWindow.Current.SwitchTilesetOffsetMode();
        }
    }

    private void txb_rom_path_TextChanged(object sender, EventArgs e)
    {
        Globals.LoadedProject.ProjectSpecificROM = txb_rom_path.Text;

        string checkPath = txb_rom_path.Text;
        if (!Path.IsPathRooted(checkPath)) checkPath = Path.Combine(Globals.ProjDirectory, checkPath);

        if (!File.Exists(checkPath))
        {
            Editor.LoadRomFromPath(Globals.RomPath, true);
            return;
        }

        Editor.LoadRomFromPath(Globals.LoadedProject.ProjectSpecificROM, true);
    }

    private void btn_select_rom_Click(object sender, EventArgs e)
    {
        string path = Editor.ShowOpenDialog("Metroid 2: Return of Samus ROM (*.gb)|*.gb");
        if (path != String.Empty) txb_rom_path.Text = path;
    }

    private void chb_fix_object_loading_CheckedChanged(object sender, EventArgs e)
    {
        Globals.LoadedProject.FixVerticalObjectLoading = chb_fix_object_loading.Checked;
    }

    private void txb_default_tile_TextChanged(object sender, EventArgs e)
    {
        Globals.LoadedProject.FillTile = (byte)Format.StringToInt(txb_default_tile.Text, 0xFF);
    }

    private void btn_assemble_compile_CheckedChanged(object sender, EventArgs e)
    {
        Globals.LoadedProject.BuildAssemblyWhenCompiling = chb_assemble_compile.Checked;
    }

    private void txb_disassembly_path_TextChanged(object sender, EventArgs e)
    {
        Globals.LoadedProject.DisassemblyPath = txb_disassembly_path.Text;

        string checkPath = txb_disassembly_path.Text;
        if (!Path.IsPathRooted(checkPath)) checkPath = Path.Combine(Globals.ProjDirectory, checkPath);

        chb_assemble_compile.Enabled = File.Exists(Path.Combine(checkPath, "build.bat"));
        if (!chb_assemble_compile.Enabled) chb_assemble_compile.Checked = false;

        //Loading ROM
        string outputPath = Path.Combine(checkPath, "out\\M2RoS.gb");
        if (File.Exists(outputPath))
        {
            txb_rom_path.Enabled = false;
            Editor.LoadRomFromPath(outputPath);
        }
        btn_select_rom.Enabled = txb_rom_path.Enabled = !File.Exists(outputPath);
    }

    private void btn_select_disassembly_Click(object sender, EventArgs e)
    {
        using (CommonOpenFileDialog ofd = new CommonOpenFileDialog() { IsFolderPicker = true })
        {
            if (ofd.ShowDialog() == CommonFileDialogResult.Ok)
                txb_disassembly_path.Text = ofd.FileName;
        }
    }
}