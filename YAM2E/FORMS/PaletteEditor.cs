using LAMP.Classes.M2_Data;
using LAMP.Classes.M2_Data.GBC;
using LAMP.Controls;
using LAMP.Utilities;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace LAMP.FORMS
{
    public partial class PaletteEditor : Form
    {
        bool init = true;
        Palette loadedPalette = null;

        public PaletteEditor(int index = 0)
        {
            InitializeComponent();

            cbb_palette.AddNumberedListContent(Globals.Palettes, "X2");
            cbb_palette.AutoSize();

            init = false;
            if (index < Globals.Palettes.Count || Globals.Palettes.Count > 0) cbb_palette.SelectedIndex = index;
        }

        private void cbb_palette_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (init) return;
            init = true;

            grp_palette.SuspendDrawing();
            grp_palette.Controls.Clear();
            if (Globals.Palettes.Count == 0)
            {
                grp_palette.ResumeDrawing();
                txb_name.Text = "";
                init = false;
                return;
            }
            Palette p = loadedPalette = Globals.Palettes[cbb_palette.SelectedIndex];
            txb_name.Text = p.Name;

            int count = 0;
            //Populate palette view
            foreach (GBColor[] colors in p.Colors)
            {
                PaletteRow pr = new(count, colors)
                {
                    Dock = DockStyle.Top,
                    Location = new Point(5000, 5000),
                };
                grp_palette.Controls.Add(pr);
                pr.ColorChanged += rowColorChanged;
                pr.BringToFront();

                count++;
            }
            grp_palette.ResumeDrawing();
            init = false;
        }

        private void rowColorChanged(object sender, EventArgs e)
        {
            PaletteRow r = sender as PaletteRow;

            //Update the colors in the current palette
            loadedPalette.Colors[r.Row][0] = r.c0;
            loadedPalette.Colors[r.Row][1] = r.c1;
            loadedPalette.Colors[r.Row][2] = r.c2;
            loadedPalette.Colors[r.Row][3] = r.c3;
        }

        private void btn_add_Click(object sender, EventArgs e)
        {
            //Generating empty palette
            Palette p = new();
            for (int i = 0; i < 8; i++)
            {
                p.Colors.Add(
                    new GBColor[] { new GBColor(0), new GBColor(0), new GBColor(0), new GBColor(0) }
                );
            }

            //Adding palette to data structures and UI
            Globals.Palettes.Add(p);
            cbb_palette.Items.Add(cbb_palette.Items.Count.ToString("X2"));
            cbb_palette.SelectedIndex = cbb_palette.Items.Count - 1;
        }

        private void txb_name_TextChanged(object sender, EventArgs e)
        {
            if (init) return;
            if (loadedPalette == null) return;

            int index = cbb_palette.SelectedIndex;
            if (loadedPalette.Name == txb_name.Text) return;

            //Prevent update
            init = true;

            loadedPalette.Name = txb_name.Text;
            string name = loadedPalette.Name != "" ? $" - {loadedPalette.Name}" : "";

            cbb_palette.Items[index] = index.ToString("X2") + name;

            cbb_palette.AutoSize();

            //Enable updating again
            init = false;
        }

        private void btn_remove_Click(object sender, EventArgs e)
        {
            if (Globals.Palettes.Count == 0) return;

            int index = cbb_palette.SelectedIndex;
            Globals.Palettes.RemoveAt(index);

            cbb_palette.Items.Clear();
            cbb_palette.AddNumberedListContent(Globals.Palettes, "X2");
            cbb_palette.AutoSize();

            cbb_palette.SelectedIndex = Math.Min(index, Globals.Palettes.Count - 1);
            cbb_palette_SelectedIndexChanged(null, null);
        }
    }
}
