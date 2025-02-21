using LAMP.Classes.M2_Data;
using LAMP.Controls.Other;
using System;
using System.Windows.Forms;

namespace LAMP.FORMS
{
    public partial class DataChunkEditor : Form
    {
        public DataChunkEditor()
        {
            InitializeComponent();

            foreach (DataChunk dc in Globals.DataChunks)
            {
                DataChunkDisplay dcd = new DataChunkDisplay(dc);
                dcd.RemoveThisChunk += PressedRemoveDataChunk;
                dcd.Dock = DockStyle.Fill;

                Panel p = new Panel()
                {
                    Padding = new Padding(3, 3, 3, 0),
                    Height = dcd.Height + 3,
                    Dock = DockStyle.Top
                };

                p.Controls.Add(dcd);
                pnl_main.Controls.Add(p);
            }
        }

        public void PressedRemoveDataChunk(object sender, EventArgs e)
        {
            DataChunkDisplay dcd = sender as DataChunkDisplay;
            Globals.DataChunks.Remove(dcd.Chunk);
            foreach (Control c in pnl_main.Controls)
            {
                if (c is not Panel) continue;
                var p = c as Panel;
                if (!p.Controls.Contains(dcd)) continue;
                p.Controls.Remove(dcd);
                pnl_main.Controls.Remove(p);
                break;
            }
        }
    }
}