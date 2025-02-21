using LAMP.Classes;
using LAMP.Properties;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LAMP.Controls.Other;

public partial class RecentFileDisplay : UserControl
{
    public RecentFileDisplay(String title, String path, bool pinned, RecentFiles parent)
    {
        InitializeComponent();

        lbl_title.Text = title;
        lbl_path.Text = path;

        btn_pin.FlatAppearance.BorderSize = 0;
        if (pinned) btn_pin.Image = Resources.pinned;
        Pinned = pinned;
        ParentView = parent;
    }

    private bool Pinned = false;
    private RecentFiles ParentView;

    public void SetBackground(Color c)
    {
        pnl_main.BackColor = c;
    }

    private void pnl_main_MouseEnter(object sender, EventArgs e)
    {
        SetBackground(Color.FromArgb(0xFF, 0xd8, 0xd8, 0xd8));
    }

    private void pnl_main_MouseLeave(object sender, EventArgs e)
    {
        SetBackground(Color.FromArgb(0xFF, 0xf0, 0xf0, 0xf0));
    }

    private void lbl_title_Click(object sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        Editor.OpenProjectAndLoad(lbl_path.Text);
    }

    private void btn_pin_Click(object sender, EventArgs e)
    {
        if (Pinned)
        {
            Globals.pinnedFiles.Remove(lbl_path.Text);
        }
        else Globals.pinnedFiles.Add(lbl_path.Text);

        ParentView.LoadRecentFileControls();
    }

    private void btn_open_path_Click(object sender, EventArgs e)
    {
        string path = Path.GetDirectoryName(lbl_path.Text);
        Process.Start("explorer.exe", path);
    }
}
