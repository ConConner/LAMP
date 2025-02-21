using System;
using System.Collections.Generic;

namespace LAMP.Utilities;

/// <summary>
/// Class for various patching formats
/// </summary>
public static class Patch
{
    #region IPS Patch
    /// <summary>
    /// Creates an IPS patch, saved as a byte[]
    /// </summary>
    /// <param name="origin">Original byte[], used as the base for the patch</param>
    /// <param name="modified">Modified byte[], used for the differences</param>
    /// <returns>IPS patch as a byte[]</returns>
    public static byte[] CreateIPSPatch(byte[] origin, byte[] modified)
    {
        List<byte> patch = new List<byte>();
        char[] HeaderData = { 'P', 'A', 'T', 'C', 'H' };
        char[] EndOfFile = { 'E', 'O', 'F' };

        //Add Header
        foreach (char c in HeaderData) patch.Add((byte)c);

        //Payload
        AddIPSPayload(origin, modified, patch);

        //End of File
        foreach (char c in EndOfFile) patch.Add((byte)c);

        return patch.ToArray();
    }
    private static void AddIPSPayload(byte[] origin, byte[] modified, List<byte> patch)
    {
        //Get length of longer file
        int len = Math.Max(origin.Length, modified.Length);
        List<byte> Records = new List<byte>();

        //Compare data
        for (int i = 0; i < len; i++)
        {
            byte? ob = origin.Length > i ? origin[i] : null;
            byte? mb = modified.Length > i ? modified[i] : null;

            int offset = i;
            ushort size = 0;
            List<byte> data = new List<byte>();

            //Check if maximum offset reached
            if (offset > 0xFFFFFF) throw new Exception("IPS file limit reached. IPS cannot fit all changes");

            //get amount of data thats different
            while (ob != mb && mb != null && size < 0xFFFF)
            {
                i++;
                size++;
                ob = origin.Length > i ? origin[i] : null;
                mb = modified.Length > i ? modified[i] : null;
                if (mb == null) i--;
                if (size == 0xFFFF) i--;
            }
            if (size > 0)
            {
                //Record Header
                //Offset
                data.Add((byte)(offset >> 16 & 0xFF));
                data.Add((byte)(offset >> 8 & 0xFF));
                data.Add((byte)(offset >> 0 & 0xFF));

                //Size
                data.Add((byte)(size >> 8 & 0xFF));
                data.Add((byte)(size >> 0 & 0xFF));

                //Data
                for (int j = 0; j < size; j++)
                {
                    data.Add(modified[offset + j]);
                }

                //Add Record to list
                Records.AddRange(data);
            }
        }

        patch.AddRange(Records);
    }


    /// <summary>
    /// Validates, that an IPS patch is correct
    /// </summary>
    /// <param name="patch"></param>
    /// <returns></returns>
    public static bool ValidateIPSPatch(byte[] patch)
    {
        //Check header
        int patchLen = patch.Length;
        if (patchLen < 8 || patch[0] != 'P' || patch[1] != 'A' || patch[2] != 'T' || patch[3] != 'C' || patch[4] != 'H') throw new Exception("Not a valid IPS file.");

        int offset = 5;
        while (offset + 2 < patchLen)
        {
            int writeOffset = (patch[offset] << 16) | (patch[offset + 1] << 8) | patch[offset + 2];
            if (writeOffset == 0x454F46) return true;
            offset += 3;
            if (offset + 1 >= patchLen) throw new Exception("Abrupt end to IPS file, entry cut off before size");

            //Read size
            int size = (patch[offset] << 8) | patch[offset + 1];
            offset += 2;
            if (size == 0)
            {
                //RLE Compressed
                if (offset + 1 > patchLen) throw new Exception("Abrupt end to IPS file, entry cut off before RLE size");
                int rleSize = (patch[offset] << 8) | patch[offset + 1];
                offset += 2;
                if (offset >= patchLen) throw new Exception("Abrupt end to IPS file, entry cut off before RLE byte");
                offset += 1;
            }
            else
            {
                if (offset + size > patchLen) throw new Exception("Abrupt end to IPS file, entry cut off before end of data block");
                offset += size;
            }
        }
        throw new Exception("Inproperly terminated IPS file");
    }

    /// <summary>
    /// Applies an IPS patch to the specified <paramref name="data"/>
    /// </summary>
    /// <param name="patch">The data of the IPS patch to apply</param>
    /// <param name="data">The data to apply the IPS patch to</param>
    /// <returns></returns>
    public static bool ApplyIPSPatch(byte[] patch, byte[] data)
    {
        throw new NotImplementedException();
    }
    #endregion

}