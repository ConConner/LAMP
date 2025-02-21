using System.Text.Json.Serialization;

namespace LAMP.Classes.M2_Data;

public class Save
{
    [JsonConstructor]
    public Save() {}

    public void setTilesetID(Tileset t)
    {
        TilesetUsed = Globals.Tilesets.IndexOf(t);

        TileGraphics = t.GfxOffset;
        MetatileData = Editor.GetMetaPointerFromTable(t.MetatileTable);
        MetatileTable = t.MetatileTable;
        CollisionData = Editor.GetCollisionPointerFromTable(t.CollisionTable);
        CollisionTable= t.CollisionTable;
        byte[] solidity = Editor.GetSolidityIndices(t.SolidityTable);
        SolidityTable = t.SolidityTable;

        SamusSolidity = solidity[0];
        EnemySolidity = solidity[1];
        ProjectileSolidity = solidity[2];
    }

    /// <summary>
    /// Writes all the data from the Save object to the specified ROM
    /// </summary>
    public void WriteToROM(Rom r)
    {
        Pointer dataStart = Rom.OffsetOf("initialSaveFile");

        //Samus' Position
        r.Write8(dataStart, SamusY);
        r.Write8(dataStart + 1, SamusScreenY);
        r.Write8(dataStart + 2, SamusX);
        r.Write8(dataStart + 3, SamusScreenX);
        //Camera Position
        r.Write8(dataStart + 4, CamY);
        r.Write8(dataStart + 5, CamScreenY);
        r.Write8(dataStart + 6, CamX);
        r.Write8(dataStart + 7, CamScreenX);
        //Tileset Definitions
        r.Write16(dataStart + 8, (ushort)SpriteGraphics.bOffset);
        r.Write8(dataStart + 10, (byte)TileGraphics.Bank);
        r.Write16(dataStart + 11, (ushort)TileGraphics.bOffset);
        r.Write16(dataStart + 13, (ushort)MetatileData.bOffset);
        r.Write16(dataStart + 15, (ushort)CollisionData.bOffset);
        r.Write8(dataStart + 17, (byte)(MapBank + 0x9));
        //Solidity Indices
        r.Write8(dataStart + 18, SamusSolidity);
        r.Write8(dataStart + 19, EnemySolidity);
        r.Write8(dataStart + 20, ProjectileSolidity);
        //Equipment
        r.Write8(dataStart + 21, Items);
        r.Write8(dataStart + 22, Beam);
        r.Write8(dataStart + 23, EnergyTanksAmount);
        r.Write8(dataStart + 24, StartEnegery);
        r.Write8(dataStart + 25, EnergyTanksFilled);
        r.Write16(dataStart + 26, MaxMissiles);
        r.Write16(dataStart + 28, StartMissiles);
        //Misc.
        r.Write8(dataStart + 31, AcidDamage);
        r.Write8(dataStart + 32, SpikeDamage);
        r.Write8(dataStart + 33, RealMetroidCount);
        r.Write8(dataStart + 34, Music);
        r.Write8(dataStart + 35, IGTMinutes);
        r.Write8(dataStart + 36, IGTHours);
        r.Write8(dataStart + 37, MetroidCount);

        //Debug Menu
        if (this.IncludeDebugMenu) r.ReplaceBytes(new int[] { Rom.OffsetOf("tryPausing.endIf_A") + 0x4, Rom.OffsetOf("gameMode_Paused.endIf") + 0xB }, new byte[] { 0x20, 0x28 }); //Enables Debug menu

        //Tweaks
        //Skip title screen
        if (SkipTitleScreen) r.Write8(Rom.OffsetOf("loadTitleScreen.endIf") + 0x9, 0x0B);

        //Skip appearance fanfare
        if (!SkipSamusFanfare) return;
        Pointer offset = Rom.OffsetOf("loadGame_samusData") + 0x6F;
        r.ReplaceBytes(new int[] { offset, offset + 0x5, offset + 0xA }, new byte[] { 0x00, 0x00, 0x00 });
    }

    /// <summary>
    /// Copies all values to another save
    /// </summary>
    public void CopyTo(Save s)
    {
        s.SamusScreenX = SamusScreenX;
        s.SamusScreenY = SamusScreenY;
        s.SamusX = SamusX;
        s.SamusY = SamusY;
        s.CamScreenX = CamScreenX;
        s.CamScreenY = CamScreenY;
        s.CamX = CamX;
        s.CamY = CamY;
        s.TilesetUsed = TilesetUsed;
        s.SpriteGraphics = SpriteGraphics;
        s.TileGraphics = TileGraphics;
        s.MetatileData = MetatileData;
        s.MetatileTable = MetatileTable;
        s.CollisionData = CollisionData;
        s.CollisionTable = CollisionTable;
        s.MapBank = MapBank;
        s.SolidityTable = SolidityTable;
        s.SamusSolidity = SamusSolidity;
        s.EnemySolidity = EnemySolidity;
        s.ProjectileSolidity = ProjectileSolidity;
        s.Items = Items;
        s.Beam = Beam;
        s.EnergyTanksAmount = EnergyTanksAmount;
        s.StartEnegery = StartEnegery;
        s.EnergyTanksFilled = EnergyTanksFilled;
        s.MaxMissiles = MaxMissiles;
        s.StartMissiles= StartMissiles;
        s.AcidDamage = AcidDamage;
        s.SpikeDamage = SpikeDamage;
        s.RealMetroidCount = RealMetroidCount;
        s.MetroidCount = MetroidCount;
        s.Music = Music;
        s.IGTHours = IGTHours;
        s.IGTMinutes = IGTMinutes;
    }

    public bool IncludeDebugMenu { get; set; } = false;
    public bool SkipSamusFanfare { get; set; } = false;
    public bool SkipTitleScreen { get; set; } = false;

    #region Save Data

    //Position Data
    //Samus
    public byte SamusScreenX { get; set; } = 0x6;
    public byte SamusScreenY { get; set; } = 0x7;
    public byte SamusX { get; set; } = 0x48;
    public byte SamusY { get; set; } = 0xD4;

    //Camera
    public byte CamScreenX { get; set; } = 0x6;
    public byte CamScreenY { get; set; } = 0x7;
    public byte CamX { get; set; } = 0x40;
    public byte CamY { get; set; } = 0xC0;

    //Level and Graphics Data
    public int TilesetUsed { get; set; } = -1;
    public Pointer SpriteGraphics { get; set; } = new Pointer(6, 0x7520); //Sprite graphics are always in B6
    public Pointer TileGraphics { get; set; } = new Pointer(7, 0x6000);
    public Pointer MetatileData { get; set; } = new Pointer(8, 0x5280);
    public int MetatileTable { get; set; } = 5;
    public Pointer CollisionData { get; set; } = new Pointer(8, 0x4580);
    public int CollisionTable { get; set; } = 4;
    public byte MapBank { get; set; } = 0x6;
    public int SolidityTable { get; set; } = 0;
    public byte SamusSolidity { get; set; } = 0x64;
    public byte EnemySolidity { get; set; } = 0x64;
    public byte ProjectileSolidity { get; set; } = 0x64;

    //Equipment
    public byte Items { get; set; } = 0;
    public byte Beam { get; set; } = 0;
    public byte EnergyTanksAmount { get; set; } = 0;
    public byte StartEnegery { get; set; } = 0x99;
    public byte EnergyTanksFilled { get; set; } = 0;
    public ushort MaxMissiles { get; set; } = 0x30;
    public ushort StartMissiles { get; set; } = 0x30;

    //Misc
    public byte AcidDamage { get; set; } = 0x2;
    public byte SpikeDamage { get; set; } = 0x8;
    public byte RealMetroidCount { get; set; } = 0x47;
    public byte MetroidCount { get; set; } = 0x39;
    public byte Music { get; set; } = 0x4;
    public byte IGTMinutes { get; set; } = 0x00;
    public byte IGTHours { get; set; } = 0x00;
    #endregion
}
