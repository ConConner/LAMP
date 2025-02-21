using LAMP.Attributes;
using System.Collections.Generic;

namespace LAMP.Classes;

public class Project
{
    //constructor
    public Project()
    {
    }

    [Setting]
    [SettingInfo(
        displayName: "Optimize Object Data", 
        description: "Allows for more objects in total.\r\n\r\nIf the object data gets optimised, it is reccommended\r\nto use the HEX tweak that changes vertical object\r\nloading,\r\n",
        category: "Objects")]
    public bool OptimizeObjectData { get; set; } = false;

    [Setting]
    [SettingInfo(
        displayName: "Fix vertical Object loading",
        description: "Fixes the vertical object loading of the\r\ngame which expects objects to be laid\r\nout in a specific way.\r\n",
        category: "Objects")]
    public bool FixVerticalObjectLoading { get; set; } = false;

    [Setting]
    [SettingInfo(
        displayName: "Use Tilesets if possible",
        description: "If Tilesets are defined for this project, they can be used instead of direct memory adresses.",
        category: "General")]
    public bool useTilesets { get; set; } = true;

    [Setting]
    [SettingInfo(
        displayName: "Project specific ROM",
        description: "The path to a ROM, which should be used as a base for this specific project. Takes precedence over the vanilla ROM.",
        category: "General")]
    public string ProjectSpecificROM { get; set; } = "";

    [Setting]
    [SettingInfo(
        displayName: "Disassembly Path",
        description: "Path to a M2RoS disassembly. The ROM from the 'out' directory will be used as a base.",
        category: "Disassembly")]
    public string DisassemblyPath { get; set; } = "";

    [Setting]
    [SettingInfo(
        displayName: "Build disassembly before compiling",
        description: "A build.bat file has to be present at the disassembly path. This will run the build.bat before compiling the ROM.",
        category: "Disassembly")]
    public bool BuildAssemblyWhenCompiling { get; set; } = false;

    [Setting]
    [SettingInfo(
        displayName: "Default Tile",
        description: "A standard tile that will be used for filling operations if nothing else is specified.",
        category: "Room Editor")]
    public byte FillTile { get; set; } = 0xFF;

    public bool EnableGBCSupport { get; set; } = false;

    public bool useAdvancedTestSettings { get; set; } = false;


    //Transition specific Data
    public List<TransitionOpcode> TransitionOpcodes { get; set; } = GenerateStandardOpcodes();

    public static List<TransitionOpcode> GenerateStandardOpcodes()
    {
        List<TransitionOpcode> result = new()
        {
            new TransitionOpcode("Fadeout [0](A)"),
            new TransitionOpcode("End Transition [0;1](FF)"),
            new TransitionOpcode("Warp [0](4), to Bank [1],, at Screen    X [3], and Screen Y [2]"),
            new TransitionOpcode("Branch to a new Transition [0](9), If Metroids alive <= [2;3], Transition Index [4;7](Transitions)"),
            new TransitionOpcode("Load Background Graphics [0;1](B1), from Bank [2;3], at Offset [4;7]"),
            new TransitionOpcode("Load Object Graphics [0;1](B2), from Bank [2;3], at Offset [4;7]"),
            new TransitionOpcode("Select metatile table [0](1), Table [1]"),
            new TransitionOpcode("Select collision table [0](2), Table [1]"),
            new TransitionOpcode("Select solidity table [0](3), Table [1]"),
            new TransitionOpcode("Change Acid & Spike damage [0](6), Acid Damage [2;3], Spike Damage [4;5]"),
            new TransitionOpcode("Load Special Graphics [0](D), Graphics Index [1]"),
            new TransitionOpcode("Load Music [0](C), Music Index [1]"),
            new TransitionOpcode("Enter Queen [0](8), Bank for current room [1], Camera X [6;9], Camera Y [2;5], Samus X [14;17], Samus Y [10;13]"),
            new TransitionOpcode("Retreat from Queen [0](5)"),
            new TransitionOpcode("Leave Queen [0](7)"),
            new TransitionOpcode("Data Block Transfer [0;1](00), Source Bank [2;3], Source Pointer [4;7], Destination Pointer [8;11], Length [12;15]"),
            new TransitionOpcode("Save BG Tileset source [0;1](01), Source Bank [2;3], Source Pointer [4;7], Destination Pointer [8;11], Length [12;15]"),
            new TransitionOpcode("Save Sprite Tileset source [0;1](02), Source Bank [2;3], Source Pointer [4;7], Destination Pointer [8;11], Length [12;15]"),
        };
        return result;
    }
}
