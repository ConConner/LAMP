using LAMP.Classes;
using LAMP.Controls.Other;
using LAMP.Interfaces;
using LAMP.Properties;
using LAMP.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace LAMP.Controls;

public partial class TransitionOpcodeDisplay : UserControl
{
    #region FIELDS
    TransitionOpcode Opcode;

    bool init = true;

    public List<byte> Data { get; set; }
    private List<OpcodeParameter> Parameters { get; set; } = new List<OpcodeParameter>();
    public int Length
    {
        get => Opcode.OpcodeLength;
    }

    public bool MoveUpEnabled
    {
        get => btn_move_up.Enabled;
        set
        {
            btn_move_up.Enabled = value;
        }
    }
    public bool MoveDownEnabled
    {
        get => btn_move_down.Enabled;
        set
        {
            btn_move_down.Enabled = value;
        }
    }

    /// <summary>
    /// Whether the parameters are shown or not
    /// </summary>
    public bool Expanded
    {
        get => pnl_parameters.Visible;
        set
        {
            if (!Opcode.isExpandable) return;

            pnl_parameters.Visible = value;
            Seperator.Visible = value;

            if (Expanded) btn_expand.Image = Resources.CollapseArrow;
            else btn_expand.Image = Resources.ExpandArrow;

            //generating the parameters if they dont exist
            if (Parameters.Count == 0) PopulateParameters();
        }
    }
    #endregion

    //CONSTRUCTOR
    public TransitionOpcodeDisplay(TransitionOpcode o)
    {
        InitializeComponent();
        pnl_parameters.BringToFront();
        Opcode = o;
        lbl_opcode_name.Text = o.Description[0];

        btn_expand.Visible = o.isExpandable;
        if (o.isExpandable) return;

        Seperator.Visible = false;
    }

    #region CUSTOM EVENTS
    public EventHandler onParameterChanged { get; set; }
    public event EventHandler ParameterChanged
    {
        add
        {
            onParameterChanged += value;
        }
        remove
        {
            onParameterChanged -= value;
        }

    }
    public virtual void OnParameterChanged(EventArgs e)
    {
        onParameterChanged?.Invoke(this, e);
    }

    public EventHandler onRemoveOpcode { get; set; }
    public event EventHandler RemoveOpcode
    {
        add
        {
            onRemoveOpcode += value;
        }
        remove
        {
            onRemoveOpcode -= value;
        }
    }

    public EventHandler onMoveOpcodeUp { get; set; }
    public event EventHandler MoveOpcodeUp
    {
        add
        {
            onMoveOpcodeUp += value;
        }
        remove
        {
            onMoveOpcodeUp -= value;
        }
    }

    public EventHandler onMoveOpcodeDown { get; set; }
    public event EventHandler MoveOpcodeDown
    {
        add
        {
            onMoveOpcodeDown += value;
        }
        remove
        {
            onMoveOpcodeDown -= value;
        }
    }
    #endregion

    private void PopulateParameters()
    {
        SuspendLayout();
        for (int i = 1; i < Opcode.Description.Length; i++)
        {
            string currentTitle = Opcode.Description[i];
            bool isList = false;
            bool isOptions = false;
            IEnumerable<INamedResource> inputList = null;
            string[] options = null;
            if (Opcode.ParameterListNames[i] != "")
            {
                //Get List object
                string listName = Opcode.ParameterListNames[i];
                inputList = typeof(Globals).GetField(listName)?.GetValue(null) as IEnumerable<INamedResource>;
                if (inputList == null) inputList = typeof(Globals).GetProperty(listName)?.GetValue(null) as IEnumerable<INamedResource>;

                //get options
                options = Opcode.ParameterListNames[i].Split(';', StringSplitOptions.TrimEntries);

                isList = inputList != null && inputList.Count() > 0;
                isOptions = options.Length > 1;
            }

            OpcodeParameter parameter = new OpcodeParameter(currentTitle, Opcode.NybbleIndices[i] != null, isList || isOptions);
            parameter.Dock = DockStyle.Top;
            parameter.Visible = true;
            pnl_parameters.Controls.Add(parameter);
            parameter.BringToFront();
            Parameters.Add(parameter);

            if (parameter.txb_Parameter != null)
            {
                //Adding event
                parameter.txb_Parameter.TextChanged += txb_parameter_TextChanged;
                parameter.txb_Parameter.Leave += txb_parameter_Leave;
            } 
            if (isList || isOptions)
            {
                parameter.cbb_ParameterList.SelectedIndexChanged += cbb_ParameterList_SelectedIndexChanged;
            }

            //Adding data
            if (Opcode.NybbleIndices[i] == null) continue;

            if (!isList && !isOptions) parameter.txb_Parameter.Text = Format.IntToString(getParameterValue(i));
            else
            {
                //Add list content to list input
                if (isList) parameter.cbb_ParameterList.AddNumberedListContent(inputList);
                if (isOptions)
                {
                    parameter.cbb_ParameterList.Items.AddRange(options);
                    parameter.cbb_ParameterList.DropDownStyle = ComboBoxStyle.DropDownList;
                }
                parameter.cbb_ParameterList.AutoSize();

                //Set current index
                parameter.cbb_ParameterList.SelectedIndex = getParameterValue(i);
            }
        }
        ResumeLayout(true);

        init = false;
    }

    private int getParameterValue(int parameterIndex)
    {
        int dataStart = (int)Opcode.NybbleIndices[parameterIndex];
        int dataLength = Opcode.ParameterLength[parameterIndex];

        int value = 0;

        for (int j = 0; j < dataLength; j++)
        {
            int writeHeadPosition = dataLength - 1 - j; //The data will be read per nybble 
            value += ByteOp.GetNybble(Data, dataStart + j) << (4 * writeHeadPosition);
        }

        //switching the bytes if length is 4 nybbles
        if (dataLength == 4) value = ByteOp.SwitchBytes(value);

        return value;
    }

    /// <summary>
    /// Returns the index of the parameter, that the input control edits
    /// </summary>
    private int findParameterIndex(object control)
    {
        for (int i = 0; i < Parameters.Count; i++)
        {
            if (Parameters[i].txb_Parameter != control && Parameters[i].cbb_ParameterList != control) continue;
            return (i + 1); // +1 since the first index is for the opcode title
        }
        return -1;
    }

    #region Events
    /// <summary>
    /// Clicked somewhere on the header to expand
    /// </summary>
    private void pnl_header_MouseClick(object sender, MouseEventArgs e)
    {
        if (!Opcode.isExpandable) return;

        //Opcode can be expandend
        Expanded = !Expanded;
    }

    /// <summary>
    /// Code for handling background color
    /// </summary>
    private void lbl_opcode_name_MouseMove(object sender, MouseEventArgs e)
    {
        if (!Opcode.isExpandable) return;
        pnl_header.BackColor = Color.FromArgb(0xFF, 0xd8, 0xd8, 0xd8);
    }
    private void lbl_opcode_name_MouseLeave(object sender, EventArgs e)
    {
        if (!Opcode.isExpandable) return;
        pnl_header.BackColor = Color.FromArgb(0xFF, 0xf0, 0xf0, 0xf0);
    }

    public void txb_parameter_TextChanged(object sender, EventArgs e)
    {
        if (init) return;

        TextBox box = (TextBox)sender;
        int boxindex = findParameterIndex(box);

        //Writing back that value
        int dataStart = (int)Opcode.NybbleIndices[boxindex];
        int dataLength = Opcode.ParameterLength[boxindex];
        int value = Format.StringToInt(box.Text, (int)Math.Pow(16, dataLength) - 1);
        if (dataLength == 4) value = ByteOp.SwitchBytes(value); //Switching if 2 bytes

        //Writing the data to the array
        for (int i = 0; i < dataLength; i++)
        {
            ByteOp.SetNybble(Data, dataStart + i, (byte)((value >> ((dataLength - 1 - i) * 4)) & 0xF));
        }

        OnParameterChanged(new EventArgs());
    }

    public void txb_parameter_Leave(object sender, EventArgs e)
    {
        TextBox box = (TextBox)sender;
        int boxindex = findParameterIndex(box);

        box.Text = Format.IntToString(getParameterValue(boxindex));
    }

    public void cbb_ParameterList_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (init) return;

        ComboBox box = (ComboBox)sender;
        int boxindex = findParameterIndex(box);

        //Writing back the value
        int dataStart = (int)Opcode.NybbleIndices[boxindex];
        int dataLength = Opcode.ParameterLength[boxindex];
        int value = box.SelectedIndex;
        if (dataLength == 4) value = ByteOp.SwitchBytes(value);

        //Writing the data to the array
        for (int i = 0; i < dataLength; i++)
        {
            ByteOp.SetNybble(Data, dataStart + i, (byte)((value >> ((dataLength - 1 - i) * 4)) & 0xF));
        }

        OnParameterChanged(new EventArgs());
    }

    private void btn_remove_opcode_Click(object sender, EventArgs e) => onRemoveOpcode?.Invoke(this, e);

    private void btn_move_down_Click(object sender, EventArgs e) => onMoveOpcodeDown?.Invoke(this, e);

    private void btn_move_up_Click(object sender, EventArgs e) => onMoveOpcodeUp?.Invoke(this, e);
    #endregion
}
