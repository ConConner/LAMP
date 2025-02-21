using System;
using System.Text.Json;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using LAMP.FORMS;
using LAMP.Classes.M2_Data;
using System.Diagnostics;
using System.Collections.Specialized;
using LAMP.Properties;
using LAMP.Utilities;
using LAMP.Actions;
using LAMP.Dialogs;

namespace LAMP.Classes;
//TODO: some of this should be put into their respective forms.
public static class Editor
{
    /// <summary>
    /// Current version being used
    /// </summary>
    public const string Version = "LAMP Beta 4.0";

    /// <summary>
    /// The current M2RoS ROM that will get used when compiling or reading Data
    /// </summary>
    public static Rom ROM { get; set; }

    /// <summary>
    /// The width of the tile selection in tiles.
    /// </summary>
    public static int SelectionWidth { get; set; } = 0;

    /// <summary>
    /// The height of the tile selection in tiles.
    /// </summary>
    public static int SelectionHeight { get; set; } = 0;

    /// <summary>
    /// The contents of the tile selection.
    /// </summary>
    public static byte[] SelectedTiles { get; set; }

    /// <summary>
    /// Checks a PasteBin (probably not the best solution) for the newest version name and opens a window if not the same.
    /// </summary>
    public static async void CheckForUpdate()
    {
        try
        {
            string latestVersion = await Program.httpClient.GetStringAsync("https://pastebin.com/6HPaBaZD");
            if (!latestVersion.Contains(Version))
            {
                //Extracting the update info from the site (Probably a super stupid way of doing things)
                string first = "{UpdateInfo}";
                string second = "{/UpdateInfo}";
                string updateData = latestVersion.Substring(latestVersion.IndexOf(first) + first.Length, latestVersion.IndexOf(second) - (latestVersion.IndexOf(first) + first.Length));
                string[] UpdateInfo = updateData.Split(",");

                //The update Info should always be laid out as: 0 = Update Title, List of features

                new UpdateAvailable(UpdateInfo).Show();
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// Creates a new project file and the needed data folders with data read from the ROM in it
    /// </summary>
    public static void CreateNewProject()
    {
        Globals.LoadedProject = null;

        //checking if a vanilla ROM exists
        if (!File.Exists(Globals.RomPath))
        {
            MessageBox.Show("No Metroid 2: Return of Samus ROM has been selected yet!", "ROM missing",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            new ProgramSettings().ShowDialog();
            return;
        }

        //ROM
        if (LoadRomFromPath(Globals.RomPath) == false) return;

        //Creating folder structure
        string projname = ShowSaveDialog("Project File (*.m2)|*.m2");
        if (projname == String.Empty) return;
        Globals.LoadedProject = new Project();
        SaveJsonObject(Globals.LoadedProject, projname);

        Globals.ProjName = projname;
        string dir = Path.GetDirectoryName(projname);
        Globals.ProjDirectory = dir;
        string dirData = dir + "/Data";
        string dirCustom = dir + "/Custom";
        Directory.CreateDirectory(dirData + "/Screens");
        Directory.CreateDirectory(dirData + "/Saves");
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
                Pointer pointer = Rom.GetPointerForArea(area) + 0x500 + 0x100 * i;
                //Pointer pointer = new Pointer(ROM.A_BANKS[area].Offset + 0x500 + 0x100 * i);
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
                Pointer offset = Rom.GetPointerForArea(area);
                //Pointer offset = ROM.A_BANKS[area];

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
                data &= 0xF7FF; //0xF7FF masks out the priority bit
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

        //Objects
        path = dirData + "/Objects.json";
        Globals.Objects = new List<List<Enemy>>();
        for (int i = 0; i < 256 * 7; i++)
        {
            Globals.Objects.Add(new List<Enemy>());
            Pointer currentPtr = new Pointer(0x3, ROM.Read16(ROM.ObjectPointerTable.Offset + 2 * i));
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
                Globals.Objects[i].Add(new Enemy(num, typ, x, y));
                count++;
            }
        }
        SaveJsonObject(Globals.Objects, path);

        //Transitions
        path = dirData + "/Transitions.json";
        Globals.Transitions = new List<Transition>();
        List<int> offsets = new List<int>(); //List saving used pointers to check for duplicate transitions
        for (int i = 0; i < 0x200; i++)
        {
            Pointer offset = new Pointer(0x5, Editor.ROM.Read16(ROM.TransitionPointerTable.Offset + (2 * i)));
            Transition t = new Transition();
            List<byte> data = new();
            //Loading transition from ROM
            for (int b = 0; b < 64; b++)
            {
                byte val = Editor.ROM.Read8(offset.Offset + b);
                data.Add(val);
                if (val == 0xFF) //End was found
                {
                    t.Data = data;
                    break;
                }
            }
            if (t.Data == null) t.Data = new List<byte>() { 0xFF }; //No end has been found, Transition is unusable so it gets replaced by an empty one
            if (offsets.Contains(offset.bOffset)) //Transition isnt unique but a copy of an existing one
            {
                int originalID = offsets.IndexOf(offset.bOffset);
                t.CopyOf = originalID;
            }
            t.Number = i;
            Globals.Transitions.Add(t);
            offsets.Add(offset.bOffset);
        }
        SaveJsonObject(Globals.Transitions, path);

        //Setting Tileset Data
        path = dirCustom + "/Tilesets.json";
        SetStandardTilesets();
        SaveJsonObject(Globals.Tilesets, path);

        //Creating the Saves
        path = dirData + "/Saves/InitialSave.json";
        SaveJsonObject(new Save(), path);
        path = dirData + "/Saves/TestROMSave.json";
        SaveJsonObject(new Save(), path);

        //New Project created
        MainWindow.Current.ProjectLoaded();
        AddRecentFile(Globals.ProjName);
        SaveRecentFiles();
        UpdateTitlebar(Globals.ProjName);
    }

    /// <summary>
    /// Prompts to open a ROM and loads it.
    /// </summary>
    public static void OpenProjectAndLoad(string path = "")
    {
        //checking if a vanilla ROM exists
        if (!File.Exists(Globals.RomPath))
        {
            MessageBox.Show("No Metroid 2: Return of Samus ROM has been selected yet!", "ROM missing",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            new ProgramSettings().ShowDialog();
            return;
        }

        if (!Hash.Compare(Globals.RomPath, Hash.Metroid2US) && Globals.ShowHashWarning)
        {
            DisplayHashMismatch(Globals.RomPath);
        }

        //ROM
        if (LoadRomFromPath(Globals.RomPath) == false) return;

        //Get the path to the Project
        if (path == "") path = ShowOpenDialog("Project file (*.m2)|*.m2");
        if (path != String.Empty) LoadProjectFromPath(path);

        //saving recent file lists
        SaveRecentFiles();
    }

    public static void DisplayHashMismatch(string romPath)
    {
        DontShowAgainDialog dialog = new DontShowAgainDialog("Mismatching Hash", $"The hash of the base ROM does not match that of vanilla Metroid 2!\r\n" +
                $"This can cause issues when loading.\r\n\r\n" +
                $"Vanilla Hash: {Hash.Metroid2US}\r\n" +
                $"Current Hash: {Hash.GetHash(romPath)}");
        dialog.DoNotShowAgainText = "Do not show again for this ROM";
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            programsettings.Default.showHashWarning = !dialog.DoNotShowAgain;
            programsettings.Default.Save();
        }
    }

    private static void SaveRecentFiles()
    {
        StringCollection recent = new StringCollection();
        recent.AddRange(Globals.recentFiles.ToArray());
        Properties.programsettings.Default.recentFiles = recent;

        StringCollection pinned = new StringCollection();
        pinned.AddRange(Globals.pinnedFiles.ToArray());
        Properties.programsettings.Default.pinnedFiles = pinned;

        Properties.programsettings.Default.Save();
    }

    /// <summary>
    /// Loads a Project from the given path.
    /// </summary>
    /// <param name="path">The path to the Project file.</param>
    public static void LoadProjectFromPath(string filepath)
    {
        AddRecentFile(filepath);

        try
        {
            string path = filepath;
            string json = File.ReadAllText(path); //reading project file
            Globals.ProjName = path;
            path = Path.GetDirectoryName(path);
            Globals.ProjDirectory = path;
            string dirData = path + "/Data";
            string dirCustom = path + "/Custom";

            //Loading Data
            Globals.LoadedProject = JsonSerializer.Deserialize<Project>(json);

            //Loading Project Specific ROM if existing
            if (Globals.LoadedProject.ProjectSpecificROM != String.Empty)
            {
                if (LoadRomFromPath(Globals.LoadedProject.ProjectSpecificROM) != true)
                {
                    MessageBox.Show("The standard ROM will be loaded!\n", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                    LoadRomFromPath(Globals.RomPath);
                }
            }
            
            //Loading Disassembly ROM
            if (Globals.LoadedProject.DisassemblyPath != String.Empty)
            {
                if (LoadRomFromPath(Globals.LoadedProject.DisassemblyPath + "\\out\\M2RoS.gb") != true)
                {
                    MessageBox.Show("The standard ROM will be loaded!\n", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    
                    LoadRomFromPath(Globals.RomPath);
                }
            }
            

            ///METROID 2 DATA
            //Screens
            Globals.Screens = new();
            for (int area = 0; area < 7; area ++)
            {
                Globals.Screens.Add(new List<GameScreen>());
                json = File.ReadAllText(dirData + $"/Screens/Area_{area}.json");
                Globals.Screens[area] = JsonSerializer.Deserialize<List<GameScreen>>(json);
            }

            //Areas
            json = File.ReadAllText(dirData + "/Areas.json");
            Globals.Areas = JsonSerializer.Deserialize<List<Area>>(json);

            //Objects
            json = File.ReadAllText(dirData + "/Objects.json");
            Globals.Objects = JsonSerializer.Deserialize<List<List<Enemy>>>(json);

            //Transitions
            json = File.ReadAllText(dirData + "/Transitions.json");
            Globals.Transitions = JsonSerializer.Deserialize<List<Transition>>(json);

            //Color Data
            if (File.Exists(dirData + "/Color/Palettes.json"))
            {
                json = File.ReadAllText(dirData + "/Color/Palettes.json");
                Globals.Palettes = JsonSerializer.Deserialize<List<Palette>>(json);
            }

            ///CUSTOM DATA
            //Tilesets
            Globals.Tilesets.Clear();
            if (File.Exists(dirCustom + "/Tilesets.json"))
            {
                json = File.ReadAllText(dirCustom + "/Tilesets.json");
                Globals.Tilesets = JsonSerializer.Deserialize<List<Tileset>>(json);
            }

            //Data Chunks
            Globals.DataChunks.Clear();
            if (File.Exists(dirCustom + "/Chunks.json"))
            {
                json = File.ReadAllText(dirCustom + "/Chunks.json");
                Globals.DataChunks = JsonSerializer.Deserialize<List<DataChunk>>(json);
            }

            //Save Data
            if (File.Exists(dirData + "/Saves/InitialSave.json"))
            {
                json = File.ReadAllText(dirData + "/Saves/InitialSave.json");
                Globals.InitialSaveGame = JsonSerializer.Deserialize<Save>(json);
                json = File.ReadAllText(dirData + "/Saves/TestROMSave.json");
                Globals.TestROMSave = JsonSerializer.Deserialize<Save>(json);
            }

            //Project loaded
            MainWindow.Current.ProjectLoaded();
            UpdateTitlebar(filepath);
        }
        catch(Exception ex)
        {
            MessageBox.Show("Something went wrong while loading the project.\n\n"+ex.Message, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

    }

    /// <summary>
    /// Adds a filepath to the recently used files list
    /// </summary>
    public static void AddRecentFile(string path)
    {
        //Add to recent files
        if (!Globals.recentFiles.Contains(path)) Globals.recentFiles.Insert(0, path);
        else
        {
            //move to front
            Globals.recentFiles.Remove(path);
            Globals.recentFiles.Insert(0, path);
        }
    }

    /// <summary>
    /// Loads a Metroid 2 ROM from the given path.  
    /// If <paramref name="silentFailure"/> is set to true, no error message will be displayed
    /// </summary>
    /// <param name="path">The path to the Metroid 2 ROM</param>
    /// <param name="silentFailure">No error message will be displayed if ROM can't be loaded</param>
    /// <returns></returns>
    public static bool LoadRomFromPath(string path, bool silentFailure = false)
    {
        try
        {
            if (!Path.IsPathRooted(path)) path = Path.Combine(Globals.ProjDirectory, path);

            ROM = new Rom(path);
            return true;
        }
        catch
        {
            if (silentFailure) return false;
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
    /// Compiles all the data files into a modified M2 ROM
    /// </summary>
    public static void CompileROM()
    {
        try
        {
            BuildAssembly();

            string filepath = ShowSaveDialog("Metroid 2: Return of Samus ROM (*.gb)|*.gb");
            if (filepath == String.Empty) return;
            SaveProject();

            //Compiling ROM
            ROM.Compile(filepath);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Something went wrong while compiling the ROM.\n\n" + ex.Message, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Compiles the ROM and then creates an IPS Patch from it
    /// </summary>
    internal static void CompileIPS()
    {
        //Check if Vanilla
        if (!Hash.Compare(Globals.BaseROMPath, Hash.Metroid2US))
        {
            MessageBox.Show($"The currently selected base ROM is not an unmodified Metroid 2: Return of Samus ROM!\n\n" +
                $"MD5 Hash is:\n{Hash.GetHash(Globals.BaseROMPath)}\n\n" +
                $"Should be:\n{Hash.Metroid2US}",
                "Mismatching Hash", MessageBoxButtons.OK, MessageBoxIcon.Error);
            new ProgramSettings().Show();
            return;
        }

        string filepath = ShowSaveDialog("IPS Patch (*.ips)|*.ips");
        if (filepath == String.Empty) return;

        //Compile ROM
        try
        {
            BuildAssembly();
            Rom vanilla = new Rom(Globals.BaseROMPath);
            ROM.Compile("");
            byte[] patchData = Patch.CreateIPSPatch(vanilla.Data, ROM.DataCopy.ToArray());

            File.WriteAllBytes(filepath, patchData);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Something went wrong while compiling the ROM.\n\n" + ex.Message, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Updates the title bar of the application to show the ROM name.
    /// </summary>
    public static void UpdateTitlebar(string path)
    {
        MainWindow.Current.Text = $"{Path.GetFileNameWithoutExtension(path)} - {Version}";
    }

    /// <summary>
    /// Saves the ROM to <see cref="Globals.ProjDirectory"/>.
    /// </summary>
    public static void SaveProject()
    {
        string dir = Globals.ProjDirectory;
        string dirData = dir + "/Data";
        string dirCustom = dir + "/Custom";

        //saving Data
        //project file
        SaveJsonObject(Globals.LoadedProject, Globals.ProjName);

        //screens
        string path = dirData + "/Screens";
        for (int area = 0; area < 7; area++)
        {
            SaveJsonObject(Globals.Screens[area], path + $"/Area_{area}.json");
        }

        //Areas
        path = dirData + "/Areas.json";
        SaveJsonObject(Globals.Areas, path);

        //Objects
        path = dirData + "/Objects.json";
        SaveJsonObject(Globals.Objects, path);

        //Transitions
        path = dirData + "/Transitions.json";
        SaveJsonObject(Globals.Transitions, path);

        //Color Data
        if (Globals.Palettes.Count != 0)
        {
            path = dirData + "/Color/";
            Directory.CreateDirectory(path);

            path = dirData + "/Color/Palettes.json";
            SaveJsonObject(Globals.Palettes, path);
        }

        ///CUSTOM
        //Tilesets
        if (Globals.Tilesets.Count != 0)
        {
            path = dirCustom + "/Tilesets.json";
            SaveJsonObject(Globals.Tilesets, path);
        }

        //DataChunks
        if (Globals.DataChunks.Count != 0)
        {
            path = dirCustom + "/Chunks.json";
            SaveJsonObject(Globals.DataChunks, path);
        }

        //Save Data
        if (!File.Exists(dirData + "/Saves/InitialSave.json"))
        {
            Directory.CreateDirectory(dirData + "/Saves");
        }
        path = dirData + "/Saves/InitialSave.json";
        SaveJsonObject(Globals.InitialSaveGame, path);
        path = dirData + "/Saves/TestROMSave.json";
        SaveJsonObject(Globals.TestROMSave, path);
    }

    /// <summary>
    /// Creates a Backup of the current ROM data in the same folder
    /// </summary>
    public static void CreateBackup()
    {
        string romName = DateTime.Now.ToString("\\/yy-MM-dd_hh-mm-ss") + ".gb";
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

    /// <summary>
    /// Returns the Screen used at the given X and Y coordinates
    /// </summary>
    public static int GetScreenNrFromXY(int x, int y, int area)
    {
        if (x >= MainWindow.Room.Width || y >= MainWindow.Room.Height) return -1;
        x /= 256;
        y /= 256;
        int screenNr = y * 16 + x;
        return Globals.Areas[area].Screens[screenNr];
    }

    /// <summary>
    /// Returns the tile id at the given X and Y coordinates
    /// </summary>
    public static byte GetTileFromXY(int x, int y, int area)
    {
        int screenNr = GetScreenNrFromXY(x, y, area);
        x %= 256;
        y %= 256;
        x /= 16;
        y /= 16;
        int tileNr = y * 16 + x;
        return Globals.Screens[area][screenNr].Data[tileNr];
    }

    /// <summary>
    /// Returns the Metatile Poiner that is stored at the given index in the table
    /// </summary>
    public static Pointer GetMetaPointerFromTable(int index)
    {
        return new Pointer(0x8, ROM.Read16(ROM.MetatilePointers.Offset + 2 * index));
    }

    /// <summary>
    /// Returns the Collision Pointer that is stored at the given index in the table
    /// </summary>
    public static Pointer GetCollisionPointerFromTable(int index)
    {
        return new Pointer(0x8, ROM.Read16(ROM.CollisionPointers.Offset + 2 * index));
    }

    /// <summary>
    /// Returns the Pointer for the selected Solidity Table
    /// </summary>
    public static Pointer GetSolidityPointerFromTable(int index)
    {
        return (ROM.SolidityIndices + 4 * index);
    }

    /// <summary>
    /// Returns an Array of Solidity Indices in the order Samus, Enemy, Projectile
    /// </summary>
    public static byte[] GetSolidityIndices(int index)
    {
        byte[] indices = new byte[3];
        indices[0] = ROM.Read8(ROM.SolidityIndices.Offset + (4 * index));
        indices[1] = ROM.Read8(ROM.SolidityIndices.Offset + (4 * index) + 1);
        indices[2] = ROM.Read8(ROM.SolidityIndices.Offset + (4 * index) + 2);

        return indices;
    }

    /// <summary>
    /// Populates the global tileset list with the standard Metroid 2 tilesets
    /// </summary>
    public static void SetStandardTilesets()
    {
        Globals.Tilesets.Clear();

        Globals.Tilesets.Add(new Tileset()
        {
            Name = "Outside Ruins",
            GfxOffset = new Pointer(141756),
            MetatileTable = 9,
            CollisionTable = 6,
            SolidityTable = 6
        });

        Globals.Tilesets.Add(new Tileset()
        {
            Name = "Inside Ruins",
            GfxOffset = new Pointer(116736),
            MetatileTable = 1,
            CollisionTable = 1,
            SolidityTable = 1
        });

        Globals.Tilesets.Add(new Tileset()
        {
            Name = "Landing Site",
            GfxOffset = new Pointer(122880),
            MetatileTable = 5,
            CollisionTable = 4,
            SolidityTable = 4
        });

        Globals.Tilesets.Add(new Tileset()
        {
            Name = "First / Last Caves",
            GfxOffset = new Pointer(120832),
            MetatileTable = 4,
            CollisionTable = 3,
            SolidityTable = 3
        });

        Globals.Tilesets.Add(new Tileset()
        {
            Name = "Final Ruins",
            GfxOffset = new Pointer(143804),
            MetatileTable = 0,
            CollisionTable = 7,
            SolidityTable = 7
        });

        Globals.Tilesets.Add(new Tileset()
        {
            Name = "Bubbles, Area 3 BrGr.",
            GfxOffset = new Pointer(114688),
            MetatileTable = 2,
            CollisionTable = 0,
            SolidityTable = 0
        });

        Globals.Tilesets.Add(new Tileset()
        {
            Name = "Caves 1, No Acid",
            GfxOffset = new Pointer(124928),
            MetatileTable = 6,
            CollisionTable = 5,
            SolidityTable = 5
        });

        Globals.Tilesets.Add(new Tileset()
        {
            Name = "Caves 1, Mid Acid",
            GfxOffset = new Pointer(124928),
            MetatileTable = 8,
            CollisionTable = 5,
            SolidityTable = 5
        });

        Globals.Tilesets.Add(new Tileset()
        {
            Name = "Caves 1, Full Acid",
            GfxOffset = new Pointer(124928),
            MetatileTable = 7,
            CollisionTable = 5,
            SolidityTable = 5
        });

        Globals.Tilesets.Add(new Tileset()
        {
            Name = "Caves 2, No Acid",
            GfxOffset = new Pointer(126256),
            MetatileTable = 6,
            CollisionTable = 5,
            SolidityTable = 5
        });

        Globals.Tilesets.Add(new Tileset()
        {
            Name = "Caves 2, Mid Acid",
            GfxOffset = new Pointer(126256),
            MetatileTable = 8,
            CollisionTable = 5,
            SolidityTable = 5
        });

        Globals.Tilesets.Add(new Tileset()
        {
            Name = "Caves 2, Full Acid",
            GfxOffset = new Pointer(126256),
            MetatileTable = 7,
            CollisionTable = 5,
            SolidityTable = 5
        });

        Globals.Tilesets.Add(new Tileset()
        {
            Name = "Caves 3, No Acid",
            GfxOffset = new Pointer(127584),
            MetatileTable = 6,
            CollisionTable = 5,
            SolidityTable = 5
        });

        Globals.Tilesets.Add(new Tileset()
        {
            Name = "Caves 3, Mid Acid",
            GfxOffset = new Pointer(127584),
            MetatileTable = 8,
            CollisionTable = 5,
            SolidityTable = 5
        });

        Globals.Tilesets.Add(new Tileset()
        {
            Name = "Caves 3, Full Acid",
            GfxOffset = new Pointer(127584),
            MetatileTable = 7,
            CollisionTable = 5,
            SolidityTable = 5
        });

        Globals.Tilesets.Add(new Tileset()
        {
            Name = "Queen",
            GfxOffset = new Pointer(118784),
            MetatileTable = 3,
            CollisionTable = 2,
            SolidityTable = 2
        });
    }

    /// <summary>
    /// Launches an Emulator window with the ROM Loaded without the TestROM form
    /// </summary>
    public static void QuickTest(Save save = null)
    {
        if (save == null) save = Globals.TestROMSave;

        try
        {
            //Build Assembly and check for Error code
            bool noErrors = BuildAssembly();
            if (Globals.LoadedProject.BuildAssemblyWhenCompiling && !noErrors)
            {
                if (MessageBox.Show("There were errors while assembling the disassembly.\n\n Do you want to continue?", "Assembly Errors", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.No) return;
            }

            string tempPath = Path.Combine(Path.GetTempPath(), "M2test.gb");
            ROM.Compile(tempPath);

            //inefficient way of making changes only to TestROM
            Rom tROM = new Rom(tempPath);

            //Applying the save file
            save.WriteToROM(tROM);

            //saving test ROM
            tROM.SaveROMAsFile(tempPath);

            ProcessStartInfo testROM = new ProcessStartInfo();
            testROM.FileName = tempPath;
            testROM.UseShellExecute = true;
            Process.Start(testROM);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Test ROM could not be launched.\n\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Adds a new DataChunk to the Global List or combines <paramref name="chunk"/> 
    /// with another chunk already existent in the List if they overlap
    /// </summary>
    public static void AddDataChunk(DataChunk chunk)
    {
        for (int i = 0; i < Globals.DataChunks.Count; i++)
        {
            DataChunk c = Globals.DataChunks[i];
            if (c.Overlap(chunk))
            {
                Globals.DataChunks[i] = c.Merge(chunk);
                return;
            }
        }
        Globals.DataChunks.Add(chunk);
    }

    /// <summary>
    /// Takes a RGBDS generated symbol file and turns it into a dictionary with proper pointer objects
    /// </summary>
    /// <param name="path">Path to a .sym file</param>
    /// <returns></returns>
    public static Dictionary<string, Pointer> ConvertSymbolToPointer(string path)
    {
        Dictionary<string, Pointer> result = new();

        string file = File.ReadAllText(path);
        string[] lines = file.Split('\n');
        for (int i = 1; i < lines.Length - 1; i++)
        {
            lines[i] = lines[i].Trim();
            string[] keyValue = lines[i].Split(' ');
            result.Add(keyValue[1], Format.StringToPointer(keyValue[0]));
        }

        return result;
    }

    /// <summary>
    /// This method will execute the build.bat of a Disassembly if a disassembly path is set and the project setting <see cref="Project.BuildAssemblyWhenCompiling"/>
    /// is set. If a ROM can be found in the out\ directory it will get loaded as the current ROM. Returns whether Build was successful or unsuccessful
    /// </summary>
    /// <exception cref="Exception">If the build.bat cannot be found but <see cref="Project.BuildAssemblyWhenCompiling"/> is still set, it will throw an exception</exception>
    public static bool BuildAssembly()
    {
        if (!Globals.LoadedProject.BuildAssemblyWhenCompiling) return false;

        string disassemblyPath = Globals.LoadedProject.DisassemblyPath;
        if (!Path.IsPathRooted(disassemblyPath)) disassemblyPath = Path.Combine(Globals.ProjDirectory, disassemblyPath);

        if (!File.Exists(Path.Combine(disassemblyPath, "build.bat"))) throw new Exception("No build.bat found for the Disassembly");
        
        //change directory
        string oldWorkDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(disassemblyPath);

        try
        {
            Process process;
            ProcessStartInfo startInfo = new(Path.Combine(disassemblyPath, "build.bat"));
            //startInfo.CreateNoWindow = true;
            process = Process.Start(startInfo);
            process.WaitForExit();

            if (process.ExitCode != 0) return false;

            //Loading new ROM
            string path = Path.Combine(disassemblyPath, "out\\M2RoS.gb");
            if (File.Exists(path)) Editor.LoadRomFromPath(path);
            else
            {
                MessageBox.Show($"No ROM found under {disassemblyPath}.\n\n" +
                                $"Make sure the ROM is in a directory named 'out' in your Disassembly root\n\n" +
                                $"The currently loaded ROM will be used!",
                    "No ROM found",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            
            //Loading symbol file
            path = Path.Combine(disassemblyPath, "out\\M2RoS.sym");
            if (File.Exists(path)) Globals.Offsets = Editor.ConvertSymbolToPointer(path);
            else
            {
                MessageBox.Show($"No symbol file found under {disassemblyPath}.\n\n" +
                                $"Make sure the symbol file is in a directory named 'out' in your Disassembly root\n\n" +
                                $"The standard offsets will be used!",
                    "No symbol file found",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Globals.Offsets = null;
            }
        }
        catch (Exception e)
        {
            throw new Exception("Error while building the Disassembly \r\n\n" + e.ToString());
        }

        Directory.SetCurrentDirectory(oldWorkDir);
        return true;
    }

    #region Borders
    /// <summary>
    /// Goes through an entire area and adds border rectangles into a list
    /// </summary>
    public static void GetScrollBorders()
    {
        Globals.ScrollBorders.Clear();
        for (int i = 0; i < 16; i++)
        {
            for (int j = 0; j < 16; j++)
            {
                int nr = j * 16 + i;
                byte scroll = Globals.Areas[Globals.SelectedArea].Scrolls[nr];

                //Checking if sides are blocked
                if (ByteOp.IsBitSet(scroll, 0)) Globals.ScrollBorders.Add(new Rectangle(i * 256 + 256 - 2, j * 256, 2, 256)); //Right
                if (ByteOp.IsBitSet(scroll, 1)) Globals.ScrollBorders.Add(new Rectangle(i * 256, j * 256, 2, 256)); //Left
                if (ByteOp.IsBitSet(scroll, 2)) Globals.ScrollBorders.Add(new Rectangle(i * 256, j * 256, 256, 2)); //Up
                if (ByteOp.IsBitSet(scroll, 3)) Globals.ScrollBorders.Add(new Rectangle(i * 256, j * 256 + 256 - 2, 256, 2)); //Down
            }
        }
    }
    #endregion

    #region Objects

    /// <summary>
    /// Adds an object at the current room location
    /// </summary>
    public static void AddObject(int x, int y, int bank)
    {
        //Coordinates for invalidation
        int xx = x * MainWindow.Room.Zoom;
        int yy = y * MainWindow.Room.Zoom;

        //Relative coordinates to the screen
        x %= 256;
        y %= 256;
        x += 8;
        y += 8;
        int screen = Globals.SelectedScreenNr + 256 * bank;
        Enemy o = new Enemy(0, 0, (byte)x, (byte)y);
        Globals.Objects[screen].Add(o);

        //Redrawing that part
        MainWindow.Room.Invalidate(new Rectangle(xx, yy, MainWindow.Room.TileSize, MainWindow.Room.TileSize));
    }

    /// <summary>
    /// Finds the first <see cref="Enemy"/> present at the given room location.
    /// </summary>
    public static Enemy FindObject(int x, int y, int bank)
    {
        x %= 256;
        y %= 256;
        int screen = Globals.SelectedScreenNr + 256 * bank;
        foreach (Enemy o in Globals.Objects[screen])
        {
            //Checking if x and y are inside a 16px rectangle of the object
            Rectangle check = new Rectangle(o.sX - 8, o.sY - 8, 16, 16);
            if (check.Contains(x, y)) return o;
        }
        return null;
    }

    public static DeleteObjectAction RemoveObjectCoordinates(int x, int y, int bank)
    {
        x %= 256;
        y %= 256;
        int screen = Globals.SelectedScreenNr + 256 * bank;
        foreach (Enemy o in Globals.Objects[screen])
        {
            //Checking if x and y are inside a 16px rectangle of the object
            Rectangle check = new Rectangle(o.sX - 8, o.sY - 8, 16, 16);
            if (check.Contains(x, y))
            {
                return new DeleteObjectAction(o, bank, Globals.SelectedScreenNr, MainWindow.Room);
            }
        }
        return null;
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

    public static Bitmap DrawColored8x8Tile(Color c)
    {
        Bitmap result = new Bitmap(8, 8);
        using (Graphics g = Graphics.FromImage(result))
        {
            g.Clear(c);
        }
        return result;
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
                if (!ByteOp.IsBitSet(lowByte, 7 - j) && !ByteOp.IsBitSet(topByte, 7 - j)) bpm.SetPixel(x + j, y + i, Globals.ColorBlack);       //0x0 = Black
                if (ByteOp.IsBitSet(lowByte, 7 - j) && !ByteOp.IsBitSet(topByte, 7 - j)) bpm.SetPixel(x + j, y + i, Globals.ColorLightGray);    //0x2 = Light Gray
                if (!ByteOp.IsBitSet(lowByte, 7 - j) && ByteOp.IsBitSet(topByte, 7 - j)) bpm.SetPixel(x + j, y + i, Globals.ColorWhite);        //0x1 = White
                if (ByteOp.IsBitSet(lowByte, 7 - j) && ByteOp.IsBitSet(topByte, 7 - j)) bpm.SetPixel(x + j, y + i, Globals.ColorDarkGray);      //0x3 = Dark Gray
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
        if (ROM.Read8(metaOffset + 0) <= 0x7F) DrawTile8(gfxOffset + 16 * ROM.Read8(metaOffset + 0), bpm, x, y);
        else DrawBlack8(bpm, x, y);
        if (ROM.Read8(metaOffset + 1) <= 0x7F) DrawTile8(gfxOffset + 16 * ROM.Read8(metaOffset + 1), bpm, x + 8, y);
        else DrawBlack8(bpm, x + 8, y);
        if (ROM.Read8(metaOffset + 2) <= 0x7F) DrawTile8(gfxOffset + 16 * ROM.Read8(metaOffset + 2), bpm, x, y + 8);
        else DrawBlack8(bpm, x, y + 8);
        if (ROM.Read8(metaOffset + 3) <= 0x7F) DrawTile8(gfxOffset + 16 * ROM.Read8(metaOffset + 3), bpm, x + 8, y + 8);
        else DrawBlack8(bpm, x + 8, y + 8);
    }

    public static Bitmap DrawTileSet(Pointer gfxOffset, Pointer metaOffset, int tilesWide, int tilesHigh, bool updateMain = true)
    {
        int count = 0;
        Bitmap[] Metatiles = new Bitmap[128]; //List of tile bitmaps
        for (int i = 0; i < tilesHigh; i++) //Filling list
        {
            for (int j = 0; j < tilesWide; j++)
            {
                Metatiles[count] = new Bitmap(16, 16);
                DrawMetaTile(gfxOffset.Offset, metaOffset.Offset + count * 4, Metatiles[count], 0, 0);
                count++;
            }
        }

        //Drawing the contents of the list onto a tileset bitmap
        Bitmap tileset = new Bitmap(16 * tilesWide, 16 * tilesHigh);
        Graphics g = Graphics.FromImage(tileset);
        count = 0;
        for (int i = 0; i < tilesHigh; i++)
        {
            for (int j = 0; j < tilesWide; j++)
            {
                g.DrawImage(Metatiles[count], new Point(16 * j, 16 * i));
                count++;
            }
        }
        g.Dispose();

        if (updateMain)
        {
            for (int i = 0; i < Metatiles.Length; i++)
            {
                if (Globals.Metatiles[i] != null) Globals.Metatiles[i].Dispose();
                Globals.Metatiles[i] = (Bitmap)Metatiles[i].Clone();
                Metatiles[i].Dispose();
            }
        }

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
                if (s.Data[counter] <= 0x7F) g.DrawImage(Globals.Metatiles[s.Data[counter]], new Point(16 * j, 16 * i));
                else g.FillRectangle(new SolidBrush(Color.DarkRed), new Rectangle(16 * j, 16 * i, 16 * j + 16, 16 * i + 16));
                counter++;
            }
        }
        g.Dispose();
        s.Image = screen;
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
                if (screen >= 0) g.DrawImage(Globals.Screens[bankNr][screen].Image, screenPoint);
                count++;
            }
        }
        g.Dispose();
    }

    public static Bitmap DrawNumberAsTile(byte value) 
    {
        int lowdigit = value & 0xF;
        int highdigit = (value & 0xF0) >> 4;

        Bitmap result = new Bitmap(16, 16);
        Image numbers = Resources.Numbers;
        Graphics g = Graphics.FromImage(result);

        //First digit
        Rectangle digit = new Rectangle(highdigit * 7, 0, 7, 9);
        g.DrawImage(numbers, 1, 3, digit, GraphicsUnit.Pixel);
        //Second digit
        digit = new Rectangle(lowdigit * 7, 0, 7, 9);
        g.DrawImage(numbers, 8, 3, digit, GraphicsUnit.Pixel);

        numbers.Dispose();
        g.Dispose();
        return result;
    }
    #endregion
}