using System;
using System.ComponentModel.Design;
using System.Windows.Forms;
using LAMP.Classes;
using LAMP.Utilities;

namespace LAMP.FORMS;

public partial class DataViewer : Form
{
    public DataViewer()
    {
        InitializeComponent();

        viewer = new ByteViewer();
        grp_data.Controls.Add(viewer);
        viewer.Dock = DockStyle.Fill;
    }

    private ByteViewer viewer { get; set; }

    private void btn_view_Click(object sender, EventArgs e)
    {
        Pointer offset = Format.StringToPointer(txb_offset.Text);
        int len = Format.StringToInt(txb_length.Text);

        txb_offset.Text = Format.PointerToString(offset);
        txb_length.Text = Format.IntToString(len);

        //populating data
        byte[] data = new byte[len];
        for (int i = 0; i < len; i++)
        {
            data[i] = Editor.ROM.Read8(offset.Offset + i);
        }

        viewer.SetBytes(data);
    }
}
