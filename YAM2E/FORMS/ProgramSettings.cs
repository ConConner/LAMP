using System;
using System.Windows.Forms;
using LAMP.Classes;
using System.IO;
using LAMP.Utilities;

namespace LAMP.FORMS;

public partial class ProgramSettings : Form
{
    bool init = false;

    public ProgramSettings()
    {
        InitializeComponent();
        init = true;
        txb_rom_path.Text = Globals.RomPath;

        //Set radio buttons
        if (Globals.hexPrefix == "" && Globals.hexSuffix == "") rbt_no_indication.Checked = true;
        if (Globals.hexPrefix == "0x") rbt_0x_prefix.Checked = true;
        if (Globals.hexPrefix == "$") rbt_dollar_prefix.Checked = true;
        if (Globals.hexSuffix == "h") rbt_h_suffix.Checked = true;

        if (Globals.bankOffsets) rbt_bank_style.Checked = true;
        else rbt_offset_style.Checked = true;

        SetCompilationChecks();
        init = false;
    }

    private void btn_select_rom_Click(object sender, EventArgs e)
    {
        string path = Editor.ShowOpenDialog("Metroid 2: Return of Samus ROM (*.gb)|*.gb");
        if (path != String.Empty) txb_rom_path.Text = path;
    }

    private void txb_rom_path_TextChanged(object sender, EventArgs e)
    {
        if (init) return;

        Globals.BaseROMPath = Globals.RomPath = txb_rom_path.Text;
        Properties.programsettings.Default.ROMPath = Globals.RomPath;

        if (!Hash.Compare(Globals.RomPath, Hash.Metroid2US) && File.Exists(Globals.RomPath))
        {
            Properties.programsettings.Default.showHashWarning = true;
            Editor.DisplayHashMismatch(Globals.RomPath);
        }

        Properties.programsettings.Default.Save();
    }

    private void clearIndication()
    {
        Globals.hexPrefix = "";
        Globals.hexSuffix = "";
    }

    private void SaveSettings()
    {
        Properties.programsettings.Default.hexPrefix = Globals.hexPrefix;
        Properties.programsettings.Default.hexSuffix = Globals.hexSuffix;
        Properties.programsettings.Default.bankOffsets = Globals.bankOffsets;
        Properties.programsettings.Default.compilerExclude = (int)Globals.CompilerExclude;

        Properties.programsettings.Default.Save();
    }

    private void SetCompilationChecks()
    {
        foreach (CheckBox chb in grp_compilation.Controls)
        {
            CompilationItem tag = (CompilationItem)Enum.Parse(typeof(CompilationItem), (string)chb.Tag);
            chb.Checked = (Globals.CompilerExclude & tag) == 0;
        }
    }

    private void chb_compilation_itemsCheckedChanged(object sender, EventArgs e)
    {
        CheckBox chb = (CheckBox)sender;
        CompilationItem tag = (CompilationItem)Enum.Parse(typeof(CompilationItem), (string)chb.Tag);
        Globals.CompilerExclude = chb.Checked ? Globals.CompilerExclude & ~tag : Globals.CompilerExclude | tag;

        SaveSettings();
    }

    private void rbt_no_indication_CheckedChanged(object sender, EventArgs e)
    {
        clearIndication();
        if (rbt_no_indication.Checked) SaveSettings();
    }

    private void rbt_0x_prefix_CheckedChanged(object sender, EventArgs e)
    {
        clearIndication();
        if (rbt_0x_prefix.Checked)
        {
            Globals.hexPrefix = "0x";
            SaveSettings();
        }
    }

    private void rbt_dollar_prefix_CheckedChanged(object sender, EventArgs e)
    {
        clearIndication();
        if (rbt_dollar_prefix.Checked)
        {
            Globals.hexPrefix = "$";
            SaveSettings();
        }
    }

    private void rbt_h_suffix_CheckedChanged(object sender, EventArgs e)
    {
        clearIndication();
        if (rbt_h_suffix.Checked)
        {
            Globals.hexSuffix = "h";
            SaveSettings();
        }
    }

    private void rbt_bank_style_CheckedChanged(object sender, EventArgs e)
    {
        SaveSettings();
        if (rbt_bank_style.Checked) Globals.bankOffsets = true;
    }

    private void rbt_offset_style_CheckedChanged(object sender, EventArgs e)
    {
        if (rbt_offset_style.Checked) Globals.bankOffsets = false;
        SaveSettings();
    }
}
