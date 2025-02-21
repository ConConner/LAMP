using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using LAMP.Classes.M2_Data;
using System.Windows.Forms;
using System.Linq;
using LAMP.Classes.M2_Data.GBC;

namespace LAMP.Classes;

public class Rom
{
    public int Size => Data.Length;
    /// <summary>
    /// Data of the ROM. Should always be unmodified!
    /// </summary>
    public readonly byte[] Data;

    /// <summary>
    /// Copy of the <see cref="Data"/>, which can be modified and saved with <see cref="SaveROMAsFile(string)"/>.
    /// </summary>
    public List<byte> DataCopy { get; private set; }

    public string Filepath;
    
    public static Pointer OffsetOf(string key)
    {
        //Check if symbol even exists. If not, throw an exception
        if (!ContainsSymbol(key)) throw new Exception($"Symbol not found. No pointer can be associated with the symbol:\n\n{key}");

        if (Globals.Offsets != null && Globals.Offsets.ContainsKey(key)) return Globals.Offsets[key];
        return StandardOffsets[key];
    }

    /// <summary>
    /// Checks if a symbol is currently loaded in either the global or standard symbol dictionary
    /// </summary>
    public static bool ContainsSymbol(string key)
    {
        return (Globals.Offsets != null && Globals.Offsets.ContainsKey(key)) || StandardOffsets.ContainsKey(key);
    }

    // constructor
    public Rom(string filename)
    {
        Data = File.ReadAllBytes(filename);
        DataCopy = Data.ToList();
        Filepath = filename;

        // check title and code
        string title = ReadAscii(0x134, 0x8);

        if (title != "METROID2")
        {
            throw new IOException("File is not a valid Metroid II: Return of Samus ROM.");
        }
    }

    /// <summary>
    /// Compiles all the data from the Editor to a copy of the base <see cref="Rom"/>.
    /// </summary>
    /// <param name="filename"></param>
    public void Compile(string filename)
    {
        //Fresh copy of the Data
        DataCopy = Data.ToList();

        //This holds the info on items that should not be compiled as per the users choice
        CompilationItem exceptions = Globals.CompilerExclude;

        #region Screens
        if ((exceptions & CompilationItem.Screens) == 0)
        {
            for (int area = 0; area < 7; area++)
            {
                for (int i = 0; i < 59; i++)
                {
                    Pointer pointer = GetPointerForArea(area) + 0x500 + 0x100 * i;
                    ReplaceBytes(pointer.Offset, Globals.Screens[area][i].Data);
                }
            }
        }
        #endregion

        #region Areas
        if ((exceptions & CompilationItem.Areas) == 0)
        {
            for (int area = 0; area < 7; area++)
            {
                Area a = Globals.Areas[area];
                for (int i = 0; i < 256; i++)
                {
                    Pointer offset = Rom.GetPointerForArea(area);

                    //Screens used
                    int data = a.Screens[i];
                    data *= 0x100;
                    data += 0x4500;
                    Write16(offset.Offset + 2 * i, (ushort)data);

                    //Scroll data
                    data = a.Scrolls[i];
                    Write8(offset.Offset + 0x200 + i, (byte)data);

                    //Transition and Priorities data
                    data = a.Tansitions[i];
                    if (a.Priorities[i]) data |= 0x800;
                    Write16(offset.Offset + 0x300 + 2 * i, (ushort)data);
                }
            }
        }
        #endregion

        #region Objects
        Pointer lastAdd = OffsetOf("enemyBank9_00");
        if ((exceptions & CompilationItem.Objects) == 0)
        {
            //if object data gets optimized there needs to be a single empty object list at the beginning
            if (Globals.LoadedProject.OptimizeObjectData) Write8(OffsetOf("enemyDataPointers"), 0xFF);

            for (int i = 0; i < 256 * 7; i++)
            {
                //Empty object list
                if (Globals.Objects[i].Count == 0)
                {
                    if (Globals.LoadedProject.OptimizeObjectData) Write16(OffsetOf("enemyDataPointers") + 2 * i, (ushort)OffsetOf("enemyBank9_00").bOffset); //Writing pointer to list
                    else
                    {
                        lastAdd += 1;
                        Write16(OffsetOf("enemyDataPointers") + 2 * i, (ushort)lastAdd.bOffset);
                        Write8(lastAdd.Offset, 0xFF);
                    }
                }
                else //Objects on screen
                {
                    lastAdd += 1;
                    Write16(OffsetOf("enemyDataPointers") + 2 * i, (ushort)lastAdd.bOffset); //Writing pointer to list

                    //Get current screens object list and sort it
                    List<Enemy> enemies = Globals.Objects[i].OrderBy(o => o.sPos).ToList();

                    foreach (Enemy o in enemies)
                    {
                        //Writing Object list consecutively
                        Write8(lastAdd.Offset, o.Number);
                        lastAdd += 1;
                        Write8(lastAdd.Offset, o.Type);
                        lastAdd += 1;
                        Write8(lastAdd.Offset, o.sX);
                        lastAdd += 1;
                        Write8(lastAdd.Offset, o.sY);
                        lastAdd += 1;
                    }
                    Write8(lastAdd.Offset, 0xFF);
                }
            }
            if (lastAdd >= OffsetOf("enemyData_end")) MessageBox.Show($"The amount of object data is exceeding the reserved space in the ROM!\n\nThe ROM might get corrupted.", "Too many objects", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        //Object loading tweak
        if (Globals.LoadedProject.FixVerticalObjectLoading)
        {
            ReplaceBytes(new Pointer(0xC135), new byte[] { 0xC3, 0xAD, 0x7D, 0x00, 0x00, 0x00, 0x00 });
            ReplaceBytes(new Pointer(0xFDAD), new byte[] { 0xFA, 0x13, 0xC4, 0x4F, 0xFA, 0x15, 0xC4, 0xB9, 0xC8, 0x79, 0xCD, 0xC1, 0x42, 0xCD, 0xCF, 0x42, 0xC3, 0x3C, 0x41 });
        }
        #endregion

        #region Transitions
        if ((exceptions & CompilationItem.Transitions) == 0)
        {
            lastAdd = OffsetOf("door000");
            lastAdd += 1;
            List<int> offsets = new List<int>(); //List saving written pointers for duplicate tansitions
            for (int i = 0; i < 0x200; i++)
            {
                Transition t = Globals.Transitions[i]; //Transition is empty / ends instantly
                if (t.Data.Count == 1)
                {
                    Write16(OffsetOf("doorPointerTable") + (2 * i), (ushort)OffsetOf("door000").bOffset); //Writing pointer to list
                    offsets.Add(OffsetOf("door000").bOffset);
                }
                else if (t.CopyOf != -1) //Transition is a duplicate and the data is already written
                {
                    Write16(OffsetOf("doorPointerTable") + (2 * i), (ushort)offsets[t.CopyOf]); //Writing pointer to list
                    offsets.Add(offsets[t.CopyOf]);
                }
                else //Transition is used and unique
                {
                    Write16(OffsetOf("doorPointerTable") + (2 * i), (ushort)lastAdd.bOffset); //Writing pointer to list
                    offsets.Add(lastAdd.bOffset);
                    //Writing transition
                    ReplaceBytes(lastAdd.Offset, t.Data);
                    lastAdd += t.Data.Count;
                }
            }
            if (lastAdd >= OffsetOf("doorData_end")) MessageBox.Show($"The amount of transition data is exceeding the reserved space in the ROM!\n\nThe ROM might get corrupted.", "Too many objects", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        #endregion
        
        #region Palettes
        if ((exceptions & CompilationItem.Palette) == 0 && Globals.Palettes.Count != 0 && Globals.LoadedProject.EnableGBCSupport)
        {
            for (int i = 0; i < Globals.Palettes.Count; i++)
            {
                Palette p = Globals.Palettes[i];
                Pointer offset = OffsetOf("paletteData") + (i * 0x40);

                foreach (GBColor[] l in p.Colors)
                    foreach (GBColor c in l)
                    {
                        Write16(offset, c.Value);
                        offset += 2;
                    }
            }
        }
        #endregion

        #region initialSaveFile
        if ((exceptions & CompilationItem.Save) == 0) Globals.InitialSaveGame.WriteToROM(this);
        #endregion

        #region DataChunks
        if ((exceptions & CompilationItem.DataChunk) == 0)
        {
            foreach (DataChunk dc in Globals.DataChunks) if (dc.Include) dc.Write(this);
        }
        #endregion

        if (filename != "") SaveROMAsFile(filename);
    }

    public void SaveROMAsFile(string filepath)
    {
        File.WriteAllBytes(filepath, DataCopy.ToArray());
    }

    #region read/write

    public byte Read8(int offset)
    {
        foreach (DataChunk c in Globals.DataChunks)
        {
            if (!c.Include) continue;
            if (c.DataStart.Offset <= offset && offset < c.DataStart.Offset + c.DataLength) //Read offset lies within a saved data chunk
            {
                //return the value from the data chunk instead of the ROM
                return c.Read8(offset);
            }
        }
        if (offset >= Size) return 0;
        return Data[offset];
    }

    public ushort Read16(int offset)
    {
        byte first = Read8(offset);
        byte second = Read8(offset + 1);
        return (ushort)(first | (second << 8));
    }

    /// <summary>
    /// Returns len amount of ASCII characters, read from the given offset.
    /// </summary>
    //TODO: probably wont return correct ASCII if used with DataChunk
    public string ReadAscii(int offset, int len)
    {
        byte[] text = new byte[len];
        Array.Copy(Data, offset, text, 0, len);
        return Encoding.ASCII.GetString(text);
    }

    /// <summary>
    /// Writes 1 byte at the given offset.
    /// </summary>
    public void Write8(int offset, byte val)
    {
        IncreaseROMSize(offset + 1);
        DataCopy[offset] = val;
    }

    /// <summary>
    /// Writes a ushort at the given offset in little endian form.
    /// </summary>
    public void Write16(int offset, ushort val)
    {
        IncreaseROMSize(offset + 2);
        DataCopy[offset] = (byte)val;
        DataCopy[offset + 1] = (byte)(val >> 8);
    }

    /// <summary>
    /// Writes the input array at the offset in ROM.
    /// </summary>
    public void ReplaceBytes(int offset, IEnumerable<byte> values)
    {
        IncreaseROMSize(offset + values.Count() + 1);
        DataCopy.RemoveRange(offset, values.Count());
        DataCopy.InsertRange(offset, values);
    }
    
    /// <summary>
    /// Writes a byte from the list for each offset in the list.
    /// </summary>
    public void ReplaceBytes(int[] offsets, byte[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            Write8(offsets[i], values[i]);
        }
    }

    /// <summary>
    /// Reads amount bytes from the offset in the ROM and writes them to dstArray
    /// </summary>
    public void ReadBytes(int offset, byte[] dstArray, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            dstArray[i] = Read8(offset + i);
        }
    }

    public static Pointer GetPointerForArea(int area)
    {
        if (Globals.LoadedProject == null) return A_BANKS[area];
        return OffsetOf("Areas") + area * 0x4000;
    }
    #endregion  

    /// <summary>
    /// Increases the size of the ROM up to the given pointer
    /// </summary>
    /// <param name="pointer"></param>
    private bool IncreaseROMSize(int pointer)
    {
        if (pointer < DataCopy.Count) return false;

        DataCopy.AddRange(Enumerable.Repeat<byte>(0x0, pointer - DataCopy.Count));

        return true;
    }

    ///CONSTANTS used for Reading
    //Areas
    public static Pointer[] A_BANKS { get; } = { new Pointer(0x24000), new Pointer(0x28000), new Pointer(0x2C000), new Pointer(0x30000), new Pointer(0x34000), new Pointer(0x38000), new Pointer(0x3C000) };

    //object data
    public Pointer ObjectPointerTable { get; } = new Pointer(0x3, 0x42E0); //6 Tables of Pointers to object lists

    //transition data
    public Pointer TransitionPointerTable { get; } = new Pointer(0x5, 0x42E5); //Table of 512 or 0x200 pointers to transition codes

    //Graphics and Solidity
    public Pointer MetatilePointers { get; } = new Pointer(0x8, 0x7F1A);
    public Pointer CollisionPointers { get; } = new Pointer(0x8, 0x7EEA);
    public Pointer SolidityIndices { get; } = new Pointer(0x8, 0x7EFA);


    //Dictionary used if no symvbol file is loaded
    private static readonly Dictionary<string, Pointer> StandardOffsets = new()
    {
        { "Areas", new Pointer(0x24000) },

        { "enemyDataPointers", new Pointer(0x3, 0x42E0) },      //6 Tables of Pointers to object lists
        { "enemyBank9_00", new Pointer(0x3, 0x50E0) },          //Lists of objects on screen, first entry should always be empty
        { "enemyData_end", new Pointer(0x3, 0x6300) },    //This  is the first byte of new data that should not be overwritten! End of object Data

        { "doorPointerTable", new Pointer(0x5, 0x42E5) },   //Table of 512 or 0x200 pointers to transition codes
        { "door000", new Pointer(0x5, 0x46E5) },            //Table of <512 Transitions, first one should stay 0xFF
        { "doorData_end", new Pointer(0x5, 0x55A3) },     //This is the first byte of new data that should not be overwritten!

        { "metatilePointerTable", new Pointer(0x8, 0x7F1A) },
        { "collisionPointerTable", new Pointer(0x8, 0x7EEA) },
        { "solidityIndexTable", new Pointer(0x8, 0x7EFA) },

        { "initialSaveFile", new Pointer(0x1, 0x4E64) },

        { "tryPausing.endIf_A", new Pointer(0x2CB2) },      //Pointers for enabling debug
        { "gameMode_Paused.endIf", new Pointer(0x2CF7) },   //

        { "loadTitleScreen.endIf", new Pointer(0x5, 0x40E3) },  //Pointer for skipping title screen
        { "loadGame_samusData", new Pointer(0x0, 0xCA3) },      //Pointer that for skipping fanfare
    };
}