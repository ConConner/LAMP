using System.Collections.Generic;

namespace LAMP;

public static class ByteOp
{
    public static bool IsBitSet(byte b, int bit) => (b & (1 << bit)) != 0;
    public static byte SetBit(byte b, int bit, int val)
    {
        bit %= 8;
        val %= 2;

        byte bitMask = (byte)(1 << bit);
        return (byte)(val == 1 ? b | bitMask : b & ~bitMask);
    }
    public static byte FlipBit(byte b, int bit) => (byte)(b ^ (1 << bit));

    /// <summary>
    /// Returns the selected nybble of the byte. 0 Indexed, Left to Right
    /// </summary>
    public static byte GetNybbleLR(byte b, int index) => (byte)((b & (0xF0 >> (4 * index))) >> (4 *  (1 - index)));
    /// <summary>
    /// Returns the selected nybble of the byte. O indexed, Right ot Left
    /// </summary>
    /// <param name="b"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static byte GetNybble(byte b, int index) => (byte)(b >> (4 * index) & 0xF);

    /// <summary>
    /// Returns the selected nybble from an array of bytes
    /// </summary>
    public static byte GetNybble(List<byte> b, int index) => GetNybbleLR(b[index / 2], index % 2);

    /// <summary>
    /// Returns the first two bytes of an int value in reverse order
    /// </summary>
    public static int SwitchBytes(int input)
    {
        byte least = (byte)(input & 0xFF);
        byte most = (byte)(input >> 8 & 0xFF);
        return least << 8 | most;
    }

    public static byte SetNybbleLR(byte b, int index, byte value)
    {
        //clear original nybble
        byte mask = (byte)~(0xF0 >> (4 * index));
        byte clearedByte = (byte)(b & mask);

        //move data
        value <<= (4 * (1 - index));
        return (byte)(clearedByte | value);
    }

    public static void SetNybble(List<byte> b, int index, byte value)
    {
        int listIndex = index / 2;
        b[listIndex] = SetNybbleLR(b[listIndex], index % 2, value);
    }
}