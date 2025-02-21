using System;
using System.Windows.Forms;
using LAMP.Classes;

namespace LAMP.FORMS;

public partial class ObjectSettings : Form
{
    public ObjectSettings(Enemy enemy)
    {
        InitializeComponent();

        o = enemy;
        num_obj_number.Value = enemy.Number;
        num_obj_type.Value = enemy.Type;
    }

    Enemy o;

    private void button1_Click(object sender, EventArgs e)
        => this.Close();

    private void num_obj_type_ValueChanged(object sender, EventArgs e)
    {
        btn_apply.Enabled = true;
    }

    private void num_obj_number_ValueChanged(object sender, EventArgs e)
    {
        btn_apply.Enabled = true;
    }

    private void btn_apply_Click(object sender, EventArgs e)
    {
        o.Number = (byte)num_obj_number.Value;
        o.Type = (byte)num_obj_type.Value;
        btn_apply.Enabled = false;
    }
}
