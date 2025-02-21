using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using LAMP.Classes;
using LAMP.FORMS;
using System.Collections.Specialized;

namespace LAMP.Controls.Other;

public partial class RecentFiles : UserControl
{
    public RecentFiles()
    {
        InitializeComponent();

        LoadRecentFileControls();
    }

    public void LoadRecentFileControls()
    {
        SuspendLayout();

        pnl_recent_files.Controls.Clear();
        List<string> obsoletes = new List<string>(); //List which holds the non existing projects

        #region Lbl for pinned and recent
        Label pinned = new Label();
        pinned.Text = "Pinned";
        pinned.Font = new Font("Century Gothic", 12, FontStyle.Bold);
        pinned.Dock = DockStyle.Top;
        pnl_recent_files.Controls.Add(pinned);
        pinned.BringToFront();

        Label recent = new Label();
        recent.Text = "Recent";
        recent.Font = new Font("Century Gothic", 12, FontStyle.Bold);
        recent.Dock = DockStyle.Top;
        pnl_recent_files.Controls.Add(recent);
        #endregion

        //Loading pinned files
        foreach (string path in Globals.pinnedFiles)
        {
            if (!File.Exists(path))
            {
                obsoletes.Add(path);
                continue;
            }

            //Adding recent file display control
            RecentFileDisplay display = new RecentFileDisplay(Path.GetFileNameWithoutExtension(path), path, true, this);
            Panel p = new Panel();

            //creating a base panel
            pnl_recent_files.Controls.Add(p);
            p.Dock = DockStyle.Top;
            p.Height = 66;
            p.Padding = new Padding(3, 3, 3, 3);
            p.BringToFront();

            //adding recent file display onto it
            p.Controls.Add(display);
            display.Dock = DockStyle.Fill;
        }

        #region Adding seperator
        Panel sepPnl = new Panel();
        sepPnl.Dock = DockStyle.Top;
        sepPnl.Padding = new Padding(3, 10, 3, 10);
        sepPnl.Height = 21;
        pnl_recent_files.Controls.Add(sepPnl);
        sepPnl.BringToFront();

        Seperator sep = new Seperator();
        pnl_recent_files.Controls.Add(sep);
        sep.Dock = DockStyle.Top;
        sepPnl.Controls.Add(sep);
        #endregion

        recent.BringToFront();

        //Loading recent files
        foreach (string path in Globals.recentFiles)
        {
            if (!File.Exists(path))
            {
                obsoletes.Add(path);
                continue;
            }

            //Adding recent file display control
            RecentFileDisplay display = new RecentFileDisplay(Path.GetFileNameWithoutExtension(path), path, Globals.pinnedFiles.Contains(path), this);
            Panel p = new Panel();

            //creating a base panel
            pnl_recent_files.Controls.Add(p);
            p.Dock = DockStyle.Top;
            p.Height = 66;
            p.Padding = new Padding(3, 3, 3, 3);
            p.BringToFront();

            //adding recent file display onto it
            p.Controls.Add(display);
            display.Dock = DockStyle.Fill;
        }

        foreach (string s in obsoletes)
        {
            Globals.recentFiles.Remove(s);
            Globals.pinnedFiles.Remove(s);
        }

        ResumeLayout();

        StringCollection pinnedList = new StringCollection();
        pinnedList.AddRange(Globals.pinnedFiles.ToArray());
        Properties.programsettings.Default.pinnedFiles = pinnedList;

        Properties.programsettings.Default.Save();
    }

    private void btn_create_project_Click(object sender, EventArgs e)
    {
        Editor.OpenProjectAndLoad();
    }

    private void btn_open_project_Click(object sender, EventArgs e)
    {
        Editor.CreateNewProject();
    }

    private void btn_project_settings_Click(object sender, EventArgs e)
    {
        new ProgramSettings().ShowDialog();
    }
}
