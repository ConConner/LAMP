﻿using System;
using System.Text.Json;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using YAM2E.FORMS;
using YAM2E.Classes.M2_Data;

namespace YAM2E.Classes;
//TODO: some of this should be put into their respective forms.
public static class Editor
{
    /// <summary>
    /// The ROM as a byte array.
    /// </summary>
    public static Rom ROM;

    /// <summary>
    /// Pointers to level data banks.
    /// </summary>
    public static int[] A_BANKS = { 0x24000, 0x28000, 0x2C000, 0x30000, 0x34000, 0x38000, 0x3C000 }; //pointers to level data banks

    /// <summary>
    /// The width of the tile selection in tiles.
    /// </summary>
    public static int SelectionWidth = 0;

    /// <summary>
    /// The height of the tile selection in tiles.
    /// </summary>
    public static int SelectionHeight = 0;

    /// <summary>
    /// The contents of the tile selection.
    /// </summary>
    public static byte[] SelectedTiles;

    public static void CreateNewProject()
    {
        //checking if a vanilla ROM exists
        if (!File.Exists(Globals.RomPath))
        {
            MessageBox.Show("No Metroid 2: Return to Samus ROM has been selected yet!", "ROM missing",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            new Start().Show();
            return;
        }

        //ROM
        if (LoadRomFromPath(Globals.RomPath) == false) return;

        //Creating folder structure
        string projname = ShowSaveDialog("Project File (*.m2)|*.m2");
        if (projname == String.Empty) return;
        SaveJsonObject(new Project() ,projname);

        string dir = Path.GetDirectoryName(projname);
        Globals.ProjDirectory = dir;
        string dirData = dir + "/Data";
        string dirCustom = dir + "/Custom";
        Directory.CreateDirectory(dirData + "/Screens");
        Directory.CreateDirectory(dirCustom);

        //populating Data
        //Screens
        string path = dirData + "/Screens";
        Globals.Screens = new();
        for (int area = 0; area < 7; area ++)
        {
            Globals.Screens.Add(new List<GameScreen>());
            for (int i = 0; i < 59; i++)
            {
                Pointer pointer = new Pointer(ROM.A_BANKS[area].Offset + 0x500 + 0x100 * i);
                Globals.Screens[area].Add(new GameScreen(pointer));
            }
            SaveJsonObject(Globals.Screens[area], path + $"/Area_{area}.json");
        }

        //Areas
        path = dirData + "/Areas.json";
        Globals.Areas = new();
        for (int area = 0; area < 7; area ++)
        {
            Area a = new();
            for (int i = 0; i < 256; i++)
            {
                Pointer offset = ROM.A_BANKS[area];

                //Screens used
                int data = ROM.Read16(offset.Offset + 2*i);
                data -= 0x4500;
                data /= 0x100;
                a.Screens[i] = data;

                //Scroll data
                data = ROM.Read8(offset.Offset + 0x200 + i);
                a.Scrolls[i] = (byte)data;

                //Transition data
                data = ROM.Read16(offset.Offset + 0x300 + 2 * i);
                data = data & 0xF7FF; //0xF7FF masks out the priority bit
                a.Tansitions[i] = data;

                //Priority bit
                data = ROM.Read16(offset.Offset + 0x300 + 2 * i);
                data &= 0x800;
                if (data == 0x800) a.Priorities[i] = true;
                else a.Priorities[i] = false;
            }
            Globals.Areas.Add(a);
        }
        SaveJsonObject(Globals.Areas, path);

        //New Project created
        MainWindow.Current.ProjectLoaded();
    }

    /// <summary>
    /// Prompts to open a ROM and loads it.
    /// </summary>
    public static void OpenProjectAndLoad()
    {
        //checking if a vanilla ROM exists
        if (!File.Exists(Globals.RomPath))
        {
            MessageBox.Show("No Metroid 2: Return to Samus ROM has been selected yet!", "ROM missing",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            new Start().Show();
            return;
        }

        //ROM
        if (LoadRomFromPath(Globals.RomPath) == false) return;

        //Get the path to ROM
        string path = ShowOpenDialog("Project file (*.m2)|*.m2");

        if (path != String.Empty)
            LoadProjectFromPath(path);
    }

    /// <summary>
    /// Loads a Project from the given path.
    /// </summary>
    /// <param name="path">The path to the Project file.</param>
    public static void LoadProjectFromPath(string path)
    {
        try 
        {
            path = Path.GetDirectoryName(path);
            string dirData = path + "/Data";
            string dirCustom = path + "/Custom";

            //Loading Data
            //Screens
            Globals.Screens = new();
            string json;
            for (int area = 0; area < 7; area ++)
            {
                Globals.Screens.Add(new List<GameScreen>());
                json = File.ReadAllText(dirData + $"/Screens/Area_{area}.json");
                Globals.Screens[area] = JsonSerializer.Deserialize<List<GameScreen>>(json);
            }

            //Areas
            json = File.ReadAllText(dirData + $"/Areas.json");
            Globals.Areas = JsonSerializer.Deserialize<List<Area>>(json);

            //Project loaded
            MainWindow.Current.ProjectLoaded();
            UpdateTitlebar(path);
        }
        catch(Exception ex)
        {
            MessageBox.Show("Something went wrong while loading the project.\n"+ex.Message, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

    }

    /// <summary>
    /// Loads a Metroid 2 ROM from the given path
    /// </summary>
    /// <param name="path">The path to the Metroid 2 ROM</param>
    /// <returns></returns>
    public static bool LoadRomFromPath(string path)
    {
        try
        {
            ROM = new Rom(path);

            //Changing button appearance
            Globals.RomLoaded = true;
            return true;
        }
        catch
        {
            MessageBox.Show("File is not a Metroid II: Return of Samus ROM!\n", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    /// <summary>
    /// Opens an "open" Dialog Window and returns the path to the file.
    /// </summary>
    /// <param name="filter">The file name filter string, which determines the choices
    /// that appear in the "Files of type" box in the dialog box.</param>
    /// <returns>A string containing the file name selected in the file dialog box.
    /// <see cref="String.Empty"/> if the dialog was cancelled.</returns>
    public static string ShowOpenDialog(string filter)
    {
        using OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = filter;
        openFileDialog.FilterIndex = 2;
        openFileDialog.RestoreDirectory = true;

        if (openFileDialog.ShowDialog() == DialogResult.OK)
            return openFileDialog.FileName;
        return String.Empty;
    }

    /// <summary>
    /// Open a "save" Dialog Window and returns the path to the file
    /// </summary>
    /// <param name="filter">The file name filter string, which determines the choices
    /// that appear in the "Save as file type" box in the dialog box</param>
    /// <returns>A string containing the file name selected in the file dialog box.
    /// <see cref="String.Empty"/> if the dialog was cancelled.</returns>
    public static string ShowSaveDialog(string filter)
    {
        using SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = filter;
        saveFileDialog.FilterIndex = 2;
        saveFileDialog.RestoreDirectory = true;

        if (saveFileDialog.ShowDialog() == DialogResult.OK)
            return saveFileDialog.FileName;
        return String.Empty;
    }

    /// <summary>
    /// Updates the title bar of the application to show the ROM name.
    /// </summary>
    public static void UpdateTitlebar(string path)
    {
        MainWindow.Current.Text = $"{Path.GetFileNameWithoutExtension(path)} - YAM2E";
    }

    /// <summary>
    /// Saves the ROM to <see cref="ROMPath"/>.
    /// </summary>
    public static void SaveProject()
    {

    }

    /// <summary>
    /// Creates a Backup of the current ROM data in the same folder
    /// </summary>
    public static void CreateBackup()
    {
        string romName = DateTime.Now.ToString("\\/yy-MM-dd_hh-mm-ss") + ".gb";
        ROM.Compile(Path.GetDirectoryName(ROM.Filepath) + romName);
    }

    /// <summary>
    /// (For now) Saves the Tileset definitions at the given file path
    /// </summary>
    public static void SaveEditorConfig(string filepath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filepath));

        //writing JSON file
        JsonSerializerOptions options = new JsonSerializerOptions();
        options.WriteIndented = true;
        string json = JsonSerializer.Serialize(Globals.Tilesets, options);
        File.WriteAllText(filepath, json);
    }

    /// <summary>
    /// Returns a list of Tileset definitions from the given Json file
    /// </summary>
    public static List<Tileset> ReadEditorConfig(string filepath)
    {
        string json = File.ReadAllText(filepath);
        return JsonSerializer.Deserialize<List<Tileset>>(json);
    }

    /// <summary>
    /// Saves an object serialized as JSON
    /// </summary>
    public static void SaveJsonObject(object obj, string filepath)
    {
        //writing JSON file
        JsonSerializerOptions options = new JsonSerializerOptions();
        options.WriteIndented = true;
        string json = JsonSerializer.Serialize(obj, options);
        File.WriteAllText(filepath, json);
    }

    /// <summary>
    /// This Function returns a rectangle with the most top left
    /// position of the given rectangles and the maximum width and height.
    /// </summary>
    public static Rectangle UniteRect(Rectangle rect1, Rectangle rect2)
    {
        int x = Math.Min(rect1.X, rect2.X);
        int y = Math.Min(rect1.Y, rect2.Y);
        int width = Math.Max(rect1.X + rect1.Width, rect2.X + rect2.Width) - x + 1;
        int height = Math.Max(rect1.Y + rect1.Height, rect2.Y + rect2.Height) - y + 1;
        return new Rectangle(x, y, width, height);
    }

    public static Rectangle SetValSize(Rectangle rect)
    {
        return new Rectangle(rect.X - 1, rect.Y - 1, rect.Width + 1, rect.Height + 1);
    }

    #region Objects

    /// <summary>
    /// Returns a list of all the objects in the current selected area bank.
    /// </summary>
    public static List<Enemy> ReadObjects(int aBankIndex)
    {
        List<Enemy> oList = new List<Enemy>();

        for (int i = 0; i < 256; i++)
        {
            Pointer currentPtr = new Pointer(0x3, ROM.Read16(ROM.ObjectPointerTable.Offset + 2 * i + 512 * aBankIndex));
            if (ROM.Read8(currentPtr.Offset) == 0xFF)
            {
                continue;
            }

            //Object found on screen
            int count = 0;

            while (ROM.Read8(currentPtr.Offset + count * 4) != 0xFF)
            {
                byte num = ROM.Read8(currentPtr.Offset + count * 4);
                byte typ = ROM.Read8(currentPtr.Offset + 1 + count * 4);
                byte x = ROM.Read8(currentPtr.Offset + 2 + count * 4);
                byte y = ROM.Read8(currentPtr.Offset + 3 + count * 4);
                oList.Add(new Enemy(num, typ, x, y, i));
                count++;
            }
        }

        return oList;
    }

    /// <summary>
    /// Shifts Object Data together to remove bubbles of freespace
    /// </summary>
    public static void ShiftObjectData()
    {
        for (int i = 0; i < 7 * 256; i++)
        {

        }
    }

    /// <summary>
    /// Adds an object at the current mouse location
    /// </summary>
    public static void AddObject(int x, int y, int bank)
    {
        int screen = (y / 16) * 16 + (x / 16);
        int X = (x * 16) % 256;
        int Y = (y * 16) % 256;

        Pointer scrPtr = new Pointer(0x3, ROM.Read16(ROM.ObjectPointerTable.Offset + bank * 512 + 2 * screen));
        if (scrPtr.bOffset != 0x50E0) ; //F THIS SHIT AHH I NEED SLEEP
    }
    #endregion

    #region Level Drawing
    public static void DrawBlack8(Bitmap bpm, int x, int y)
    {
        for(int i = 0; i < 8; i++)
        {
            for(int j = 0; j < 8; j++)
            {
                bpm.SetPixel(x + i, y + j, Globals.ColorBlack);
            }
        }
    }

    public static void DrawTile8(int offset, Bitmap bpm, int x, int y)
    {
        //one 8x8 tile = 16 bytes
        for (int i = 0; i < 8; i++)
        {
            //load one 8 pixel row
            //one row = 2 bytes
            byte topByte = ROM.Read8(offset + (2 * i));
            byte lowByte = ROM.Read8(offset + (2 * i) + 1);

            for (int j = 0; j < 8; j++) //looping through both bytes to generate the colours
            {
                if (!ByteOp.IsBitSet(lowByte, 7 - j) && !ByteOp.IsBitSet(topByte, 7 - j)) bpm.SetPixel(x + j, y + i, Globals.ColorBlack);
                if (ByteOp.IsBitSet(lowByte, 7 - j) && !ByteOp.IsBitSet(topByte, 7 - j)) bpm.SetPixel(x + j, y + i, Globals.ColorLightGray);
                if (!ByteOp.IsBitSet(lowByte, 7 - j) && ByteOp.IsBitSet(topByte, 7 - j)) bpm.SetPixel(x + j, y + i, Globals.ColorWhite);
                if (ByteOp.IsBitSet(lowByte, 7 - j) && ByteOp.IsBitSet(topByte, 7 - j)) bpm.SetPixel(x + j, y + i, Globals.ColorDarkGray);
            }
        }

    }

    public static void DrawTile8Set(int offset, Bitmap bpm, Point p, int tilesWide, int tilesHigh)
    {
        int count = 0;
        for (int i = 0; i < tilesHigh; i++)
        {
            for (int j = 0; j < tilesWide; j++)
            {
                DrawTile8(offset + 16 * count, bpm, p.X + 8 * j, p.Y + 8 * i);
                count++;
            }
        }
    }

    public static void DrawMetaTile(int gfxOffset, int metaOffset, Bitmap bpm, int x, int y)
    {
        if (ROM.Data[metaOffset + 0] <= 0x7F) DrawTile8(gfxOffset + 16 * ROM.Data[metaOffset + 0], bpm, x, y);
        else DrawBlack8(bpm, x, y);
        if (ROM.Data[metaOffset + 1] <= 0x7F) DrawTile8(gfxOffset + 16 * ROM.Data[metaOffset + 1], bpm, x + 8, y);
        else DrawBlack8(bpm, x + 8, y);
        if (ROM.Data[metaOffset + 2] <= 0x7F) DrawTile8(gfxOffset + 16 * ROM.Data[metaOffset + 2], bpm, x, y + 8);
        else DrawBlack8(bpm, x, y + 8);
        if (ROM.Data[metaOffset + 3] <= 0x7F) DrawTile8(gfxOffset + 16 * ROM.Data[metaOffset + 3], bpm, x + 8, y + 8);
        else DrawBlack8(bpm, x + 8, y + 8);
    }

    public static Bitmap DrawTileSet(int gfxOffset, int metaOffset, int tilesWide, int tilesHigh)
    {
        int count = 0;
        for (int i = 0; i < tilesHigh; i++)
        {
            for (int j = 0; j < tilesWide; j++)
            {
                if (Globals.Metatiles[count] != null) Globals.Metatiles[count].Dispose();
                Globals.Metatiles[count] = new Bitmap(16, 16);
                DrawMetaTile(gfxOffset, metaOffset + count * 4, Globals.Metatiles[count], 0, 0);
                count++;
            }
        }

        Bitmap tileset = new Bitmap(16 * tilesWide, 16 * tilesHigh);
        Graphics g = Graphics.FromImage(tileset);
        count = 0;
        for (int i = 0; i < tilesHigh; i++)
        {
            for (int j = 0; j < tilesWide; j++)
            {
                g.DrawImage(Globals.Metatiles[count], new Point(16 * j, 16 * i));
                count++;
            }
        }
        g.Dispose();
        return tileset;
    }

    /// <summary>
    /// Saves a Bitmap of a Screen from the screen list
    /// </summary>
    public static void DrawScreen(int area, int screenNr)
    {
        Bitmap screen = new Bitmap(256, 256);
        Graphics g = Graphics.FromImage(screen);
        GameScreen s = Globals.Screens[area][screenNr];
        int counter = 0;
        for (int i = 0; i < 16; i++)
        {
            for (int j = 0; j < 16; j++)
            {
                g.DrawImage(Globals.Metatiles[s.Data[counter]], new Point(16 * j, 16 * i));
                counter++;
            }
        }
        g.Dispose();
        if (s.image != null) s.image.Dispose();
        s.image = screen;
    }

    public static void DrawAreaBank(int bankNr, Bitmap bmp, Point p)
    {
        for (int i = 0; i < 59; i++)
        {
            DrawScreen(bankNr, i);
        }

        //drawing the areas
        Graphics g = Graphics.FromImage(bmp);
        Area a = Globals.Areas[bankNr];
        int count = 0;
        for (int i = 0; i < 16; i++)
        {
            for (int j = 0; j < 16; j++)
            {
                int screen = a.Screens[count];
                Point screenPoint = new Point(p.X + (j * 256), p.Y + (i * 256));
                if (screen >= 0) g.DrawImage(Globals.Screens[bankNr][screen].image, screenPoint);
                count++;
            }
        }
        g.Dispose();
    }

    public static void UpdateScreen(int screen, int bankOffset)
    {
        //Globals.Screens[screen] = DrawScreen(bankOffset + 0x500 + (0x100 * screen));
    }
    #endregion
}