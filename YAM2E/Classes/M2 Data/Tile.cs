using System.Drawing;

namespace LAMP.Classes.M2_Data;

/// <summary>
/// A Tile represents the smallest possible graphic that can be displayed.
/// A Tile consists of 8x8 pixels represented by 16 bytes
/// </summary>
public struct Tile
{
    /// <summary>
    /// Creates a new Tile
    /// </summary>
    public Tile() { }

    /// <summary>
    /// Creates a new Tile with the data at the given offset.
    /// A tile is represented by 16 bytes
    /// </summary>
    public Tile(Pointer offset)
    {
        for (int i = 0; i < 16; i++)
        {
            data[i] = Editor.ROM.Read8(offset.Offset + i);
        }
    }

    /// <summary>
    /// The Data representing the Tile
    /// </summary>
    public byte[] Data
    {
        get => data;
        set => data = value;
    }
    private byte[] data = new byte[16];

    #region Methods
    /// <summary>
    /// Draws the Tile as a Bitmap
    /// </summary>
    /// <returns></returns>
    public Bitmap Draw()
    {
        Bitmap result = new(8, 8);

        for (int i = 0; i < 8; i++)
        {
            //load one 8 pixel row
            //one row = 2 bytes
            byte topByte = Data[2 * i];
            byte lowByte = Data[(2 * i) + 1];

            for (int j = 0; j < 8; j++) //looping through both bytes to generate the colours
            {
                if (!ByteOp.IsBitSet(lowByte, 7 - j) && !ByteOp.IsBitSet(topByte, 7 - j)) result.SetPixel(j, i, Globals.ColorBlack);
                if (ByteOp.IsBitSet(lowByte, 7 - j) && !ByteOp.IsBitSet(topByte, 7 - j)) result.SetPixel(j, i, Globals.ColorLightGray);
                if (!ByteOp.IsBitSet(lowByte, 7 - j) && ByteOp.IsBitSet(topByte, 7 - j)) result.SetPixel(j, i, Globals.ColorWhite);
                if (ByteOp.IsBitSet(lowByte, 7 - j) && ByteOp.IsBitSet(topByte, 7 - j)) result.SetPixel(j, i, Globals.ColorDarkGray);
            }
        }

        return result;
    }

    public void SetPixel(int x, int y, int val)
    {
        val %= 4;

        byte firstByte = Data[y * 2]; //Least significant byte
        byte secondByte = Data[y * 2 + 1]; //Most significant byte

        Data[y * 2] = ByteOp.SetBit(firstByte, 7 - x, (val & 0b0001) == 0 ? 0 : 1);
        Data[y * 2 + 1] = ByteOp.SetBit(secondByte, 7 - x, (val & 0b0010) == 0 ? 0 : 1);
    }

    public void Rotate90()
    {

    }

    public void FlipVertical()
    {

    }

    public void FlipHorizontal()
    {

    }
    #endregion
}
