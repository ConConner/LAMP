using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using LAMP.Classes;
using LAMP.Classes.M2_Data;
using LAMP.Controls;
using LAMP.Controls.Transitions;
using LAMP.Utilities;

namespace LAMP.FORMS;

public partial class TransitionsEditor : Form
{
    public int TransitionLength
    {
        get => transitionLength;
        set
        {
            if (transitionLength == value) return;
            transitionLength = value;

            lbl_tred_transition_length.Text = $"Transition Length: {Format.IntToString(value)} out of {Format.IntToString(64)} bytes";
            gau_transition_length.Value = value / 64d;
        }
    }
    int transitionLength;

    int SelectedIndex
    {
        get => selectedIndex;
        set
        {
            if (selectedIndex == value) return;
            selectedIndex = value;

            LoadTransition();
        }
    }
    int selectedIndex = -1; //This is set to -1 because the index will initially be 0, therefore, we still want to trigger this once

    public List<TransitionOpcodeDisplay> Opcodes = new List<TransitionOpcodeDisplay>();

    Transition LoadedTransition
    {
        get => loadedTransition;
        set
        {
            if (loadedTransition == value) return;
            loadedTransition = value;
        }
    }
    Transition loadedTransition;

    //Constructor
    public TransitionsEditor(int TransitionIndex = 0)
    {
        InitializeComponent();
        txb_warning_header.BackColor = DefaultBackColor;

        cbb_tred_transition_selection.AddNumberedListContent(Globals.Transitions);
        cbb_tred_transition_selection.AutoSize();
        cbb_tred_transition_selection.SelectedIndex = TransitionIndex;

        LoadOpcodeList();

        //Showing tileset list
        if (!Globals.LoadedProject.useTilesets || Globals.Tilesets.Count == 0) return;
        grp_tilesets.Visible = true;

        //Updating the combobox with all the tilesets
        cbb_tileset_select.AddNumberedListContent(Globals.Tilesets, "X");
        cbb_tileset_select.SelectedIndex = Globals.Tilesets.Count - 1;
        cbb_tileset_select.AutoSize();
    }

    void LoadOpcodeList()
    {
        foreach (TransitionOpcode o in Globals.LoadedProject.TransitionOpcodes)
        {
            OpcodeTemplate temp = new OpcodeTemplate(o);
            temp.Dock = DockStyle.Top;
            temp.onAddOpcode += templateAddOpcode;
            pnl_opcodes.Controls.Add(temp);
            temp.BringToFront();
        }
    }

    void CheckIfCopy()
    {
        if (LoadedTransition.CopyOf != -1) //Transition is a duplicate
        {
            pnl_warning.Visible = true;

            string newLine = Environment.NewLine;
            txb_transition_info.Text = $"This Transition shares it's data with" + newLine + newLine +
                $"Transition {LoadedTransition.CopyOf:X3}.";
            LoadedTransition = Globals.Transitions[LoadedTransition.CopyOf];
        }
        else
        {
            pnl_warning.Visible = false;
        }
    }

    void ReadTransition()
    {
        pnlTransition.SuspendDrawing(); //Extension method that stops drawing entirely
        pnlTransition.SuspendLayout(); //Suspending the layout logic is required here because it is missing in the opcode display code

        //Set Transition name
        txb_transition_name.Text = LoadedTransition.Name;

        //Clear existing controls of prior transiition
        pnlTransition.Controls.Clear();
        Opcodes.Clear();

        //Populatig transition
        for (int i = 0; i < LoadedTransition.Data.Count;)
        {
            //Finding a matching opcode to the current byte
            TransitionOpcode? currentOpcode = null; //making the opcode nullable because there might be a chance that an index is not found

            foreach (TransitionOpcode code in Globals.LoadedProject.TransitionOpcodes)
            {
                byte currentByte = LoadedTransition.Data[i]; //The opcode Index to check for
                if (code.ParameterLength[0] == 1) currentByte = (byte)(currentByte >> 4); //An opcode index might only be on the first nybble, therefore, we have to shift the check byte to the right
                if (code.OpcodeIndex == currentByte)
                {
                    currentOpcode = code;
                    break;
                }
            }

            //Checking if an opcode was found
            if (currentOpcode == null)
            {
                //TODO: handle unassigned indices
                continue;
            }
            TransitionOpcode correctOpcode = (TransitionOpcode)currentOpcode;

            //putting data for each opcode into lists
            int length = correctOpcode.OpcodeLength;
            List<Byte> data = LoadedTransition.Data.GetRange(i, length);

            TransitionOpcodeDisplay o = new TransitionOpcodeDisplay(correctOpcode)
            {
                Dock = DockStyle.Top,
                Data = data
            };
            Opcodes.Add(o);
            pnlTransition.Controls.Add(Opcodes[Opcodes.Count - 1]);
            o.onParameterChanged += opcode_parameterChanged;
            o.onRemoveOpcode += removeOpcode;
            o.onMoveOpcodeDown += moveOpcodeDown;
            o.onMoveOpcodeUp += moveOpcodeUp;
            o.BringToFront();

            i += length;
        }

        //Setting the arrows
        SetAllMoveArrows();

        //Setting length display
        TransitionLength = LoadedTransition.Data.Count;

        pnlTransition.ResumeLayout(); //Resume layout logic
        pnlTransition.ResumeDrawing();
    }

    void LoadTransition()
    {
        try
        {
            LoadedTransition = Globals.Transitions[SelectedIndex];
            CheckIfCopy();
            ReadTransition();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error has occured while loading the Transition.\n\n{ex}\n\nMaybe the data is corrupt?", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    void SaveTransition()
    {
        LoadedTransition.Data.Clear();
        foreach (TransitionOpcodeDisplay o in Opcodes)
        {
            foreach (byte b in o.Data) LoadedTransition.Data.Add(b);
        }
    }

    void SetAllMoveArrows()
    {
        foreach (TransitionOpcodeDisplay o in Opcodes)
        {
            int index = pnlTransition.Controls.GetChildIndex(o);
            o.MoveUpEnabled = true;
            o.MoveDownEnabled = true;
            if (index == Opcodes.Count - 1) o.MoveUpEnabled = false;
            if (index == 0) o.MoveDownEnabled = false;
        }
    }

    #region Events
    private void cbb_tred_transition_selection_SelectedIndexChanged(object sender, EventArgs e)
    {
        SelectedIndex = cbb_tred_transition_selection.SelectedIndex;
    }

    private void btn_seperate_transition_Click(object sender, EventArgs e)
    {
        Transition t = Globals.Transitions[cbb_tred_transition_selection.SelectedIndex];
        t.Data = new List<byte>(LoadedTransition.Data);
        t.CopyOf = -1;
        LoadTransition();
    }

    private void btn_remove_all_ButtonClick(object sender, EventArgs e)
    {
        if (MessageBox.Show("Are you sure you want to remove all opcodes from the transition?", "Remove all opcodes?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No) return;

        Opcodes.Clear();
        SaveTransition();
        pnlTransition.Controls.Clear();
        TransitionLength = 0;
    }

    private void btn_add_tileset_Click(object sender, EventArgs e)
    {
        //Adds the standard way of loading a new tileset
        List<byte> tilesetData = new List<byte>();
        Tileset t = Globals.Tilesets[cbb_tileset_select.SelectedIndex];

        //Load Graphics Page
        tilesetData.Add(0xB1);
        tilesetData.Add((byte)t.GfxOffset.Bank);
        tilesetData.Add((byte)(t.GfxOffset.bOffset & 0xFF));
        tilesetData.Add((byte)(t.GfxOffset.bOffset >> 8));

        //Load Tables
        tilesetData.Add((byte)(0x10 | (t.MetatileTable & 0xF)));
        tilesetData.Add((byte)(0x20 | (t.CollisionTable & 0xF)));
        tilesetData.Add((byte)(0x30 | (t.SolidityTable & 0xF)));

        if (LoadedTransition.Data.Count + tilesetData.Count > 0x40)
        {
            MessageBox.Show("There is not enough space left in the Transition to load this Tileset!", "Out of space", MessageBoxButtons.OK, MessageBoxIcon.Error); return;
        }
        LoadedTransition.Data.InsertRange(0, tilesetData); //Inserting the data before the end.
        LoadTransition();
    }

    private void txb_transition_name_TextChanged(object sender, EventArgs e)
    {
        int index = SelectedIndex;
        if (LoadedTransition.Name == txb_transition_name.Text) return;

        LoadedTransition.Name = txb_transition_name.Text;
        string name = LoadedTransition.Name != "" ? $" - {LoadedTransition.Name}" : "";

        cbb_tred_transition_selection.Items[index] = index.ToString("X3") + name;

        cbb_tred_transition_selection.AutoSize();
    }

    private void opcode_parameterChanged(object sender, EventArgs e)
        => SaveTransition();

    private void removeOpcode(object sender, EventArgs e)
    {
        TransitionOpcodeDisplay o = (TransitionOpcodeDisplay)sender;

        Opcodes.Remove(o);
        pnlTransition.Controls.Remove(o);
        TransitionLength -= o.Length;
        SaveTransition();
        SetAllMoveArrows();
    }

    private void moveOpcodeUp(object sender, EventArgs e)
    {
        TransitionOpcodeDisplay o = (TransitionOpcodeDisplay)sender;

        int index = pnlTransition.Controls.GetChildIndex(o);
        pnlTransition.Controls.SetChildIndex(o, index + 1);
        Opcodes.Switch(Opcodes.Count - 1 - index, Opcodes.Count - 2 - index);

        SetAllMoveArrows();

        SaveTransition();
    }

    private void moveOpcodeDown(object sender, EventArgs e)
    {
        TransitionOpcodeDisplay o = (TransitionOpcodeDisplay)sender;

        o.MoveUpEnabled = true;
        int index = pnlTransition.Controls.GetChildIndex(o);
        pnlTransition.Controls.SetChildIndex(o, index - 1);
        Opcodes.Switch(Opcodes.Count - 1 - index, Opcodes.Count - index);

        SetAllMoveArrows();

        SaveTransition();
    }

    private void templateAddOpcode(object sender, EventArgs e)
    {
        OpcodeTemplate o = (OpcodeTemplate)sender;

        //Add the required data of the opcode
        List<byte> data = new List<byte>();

        byte index = o.opcode.OpcodeIndex;
        if (o.opcode.ParameterLength[0] == 1) index = (byte)(index << 4);
        data.Add(index);

        for (int i = 1; i < o.opcode.OpcodeLength; i++)
        {
            //TODO: add support for predefined values
            data.Add(0x0);
        }

        if (LoadedTransition.Data.Count + data.Count > 0x40)
        {
            MessageBox.Show("There is not enough space left in the Transition to load this Tileset!", "Out of space", MessageBoxButtons.OK, MessageBoxIcon.Error); return;
        }
        LoadedTransition.Data.InsertRange(0, data); //Inserting the data before the end.
        LoadTransition();
    }
    #endregion

    private void btn_export_Click(object sender, EventArgs e)
    {
        string path = Editor.ShowSaveDialog("Transition (*.lts)|*.lts");
        if (path == string.Empty) return;

        ExportTransition t = new ExportTransition()
        {
            Name = LoadedTransition.Name,
            Data = LoadedTransition.Data
        };

        Editor.SaveJsonObject(t, path);
    }

    private void btn_import_Click(object sender, EventArgs e)
    {
        string path = Editor.ShowOpenDialog("Transition (*.lts)|*.lts");
        if (!File.Exists(path)) return;

        string json = File.ReadAllText(path);
        ExportTransition file = JsonSerializer.Deserialize<ExportTransition>(json);

        if (file == null) return;
        Transition t = new Transition()
        {
            Name = file.Name,
            Data = file.Data,
            CopyOf = -1,
            Number = LoadedTransition.Number
        };

        Globals.Transitions[SelectedIndex] = t;
        LoadTransition();
    }
}