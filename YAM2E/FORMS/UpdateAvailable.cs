using System;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace LAMP.FORMS;

public partial class UpdateAvailable : Form
{
    public UpdateAvailable(string[] UpdateInfo)
    {
        InitializeComponent();

        //Title
        txb_title.Text = UpdateInfo[0];

        //Writing Update info
        StringBuilder s = new StringBuilder();
        for (int i = 1; i < UpdateInfo.Length; i++)
        {
            s.Append(UpdateInfo[i]);
            s.Append("\r\n");
        }

        txb_features.Text = s.ToString();
    }

    private void btn_remind_Click(object sender, EventArgs e)
    {
        this.Close();
    }

    private void btn_download_Click(object sender, EventArgs e)
    {
        string target = "https://github.com/ConConner/LAMP/releases";
        Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
    }
}
