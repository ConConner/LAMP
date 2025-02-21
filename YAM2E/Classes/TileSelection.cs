using System;

namespace LAMP.Classes;

[Serializable]
internal class TileSelection
{
    public TileSelection(int Width, int Height, byte[] data)
    {
        SelectionHeight = Height;
        SelectionWidth = Width;
        Tiles = data;
    }
    //Fields
    public int SelectionWidth { get; set; }
    public int SelectionHeight { get; set; }
    public byte[] Tiles { get; set; }
}
