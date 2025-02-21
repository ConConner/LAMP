using LAMP.Classes;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace LAMP.Utilities;

public static class Format
{
    //This class will have methods to interpret text 

    /// <summary>
    /// Takes a string and interpretes it into a pointer object.
    /// You can input a bank + offset or a direct offset.
    /// Does not care about pre- and suffixes.
    /// Returns a null object if the input is invalid! CHECK FOR NULL OBJECT!
    /// </summary>
    public static Pointer StringToPointer(string input)
    {
        if (input.Length == 0) return new(0);

        if (Rom.ContainsSymbol(input)) return Rom.OffsetOf(input);

        //removing spaces and pre-/suffix
        input = Regex.Replace(input, @"\s+", "");
        input = input.Replace("0x", "").Replace("h", "").Replace("$", "");
        input = input.Replace("ROM", ""); //Replacing ROM (could be copied from disassembly)

        //Splitting bank and relative offset
        string[] words = input.Split(',');
        if (words.Length == 1) words = input.Split(':');
        if (words.Length == 1) words = input.Split(';');

        //Converting to Pointer
        try
        {
            if (words.Length == 2)
            {
                int bank, offset;
                if (words[0].Length == 0) bank = 0;
                else bank = int.Parse(words[0], System.Globalization.NumberStyles.HexNumber);
                if (words[1].Length == 0) offset = 0;
                else offset = int.Parse(words[1], System.Globalization.NumberStyles.HexNumber);

                Pointer result = new Pointer(bank, offset);
                if (result < 0) return null;
                return result;
            }
            else if (words.Length == 1)
            {
                int offset;
                if (words[0].Length == 0) offset = 0;
                else offset = int.Parse(words[0], System.Globalization.NumberStyles.HexNumber);

                if (offset < 0) return null;
                return new(offset);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Takes a pointer and represents it as a string.
    /// Uses the users preffered styling for offset splits
    /// and pre- and suffixes.
    /// </summary>
    public static string PointerToString(Pointer input)
    {
        if (Globals.bankOffsets)
        {
            StringBuilder s = new();
            s.Append(Globals.hexPrefix).Append(input.Bank.ToString("X")).Append(Globals.hexSuffix);
            s.Append(", ");
            s.Append(Globals.hexPrefix).Append(input.bOffset.ToString("X")).Append(Globals.hexSuffix);
            return s.ToString();
        } 
        else
        {
            StringBuilder s = new();
            s.Append(Globals.hexPrefix).Append(input.Offset.ToString("X")).Append(Globals.hexSuffix);
            return s.ToString();
        }
    }


    /// <summary>
    /// Takes a string and interprets it as a hex integer.
    /// Only allows a direct value.
    /// Does not care about pre- and suffixes.
    /// Returns 0 if it gets an invalid input
    /// </summary>
    public static int StringToInt(string input, int maxValue = 0)
    {
        if (input.Length == 0) return 0;

        //removing spaces and pre-/suffix
        input = Regex.Replace(input, @"(\s+|0x|h|\$|ROM)", "");

        //Splitting at "splitters" to throw error at user for being a bad person
        string[] words = input.Split(',');
        if (words.Length == 1) words = input.Split(':');
        if (words.Length == 1) words = input.Split(';');

        //Converting to Int
        try
        {
            if (words.Length == 1)
            {
                int val;
                if (words[0].Length == 0) val = 0;
                else if (words[0].Length > 7) val = 0xFFFFFFF;
                else val = int.Parse(words[0], System.Globalization.NumberStyles.HexNumber);
                if (maxValue != 0) return Math.Min(val, maxValue);
                return val;
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Takes a string and interprets it as an integer.
    /// Returns 0 if it gets an invalid input
    /// </summary>
    public static int StringToDecimalInt(string input, int maxValue = 0)
    {
        if (input.Length == 0) return 0;

        //removing spaces
        input = Regex.Replace(input, @"\s+", "");

        try
        {
            return int.Parse(input);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Takes an integer and represents it as a hex string.
    /// Adds the users preffered pre- and suffix.
    /// </summary>
    public static string IntToString(int input)
    {
        StringBuilder s = new();
        s.Append(Globals.hexPrefix).Append(input.ToString("X")).Append(Globals.hexSuffix);
        return s.ToString();
    }


    //BCD Functions
    /// <summary>
    /// Takes a BCD value and turns it into a decimal value.
    /// It does not check for correctnes of the BCD value!!
    /// 
    /// This function was written with Chat-GPT.
    /// Notes: It needed one retry after the first iteration
    /// gave the wrong results. It figured the error out by itself.
    /// </summary>
    public static int BcdToInt(int bcdValue)
    {
        int result = 0;
        int factor = 1;

        // Iterate over the BCD digits from right to left
        while (bcdValue != 0)
        {
            // Extract the rightmost BCD digit
            int digit = bcdValue & 0x0F;

            // Convert the BCD digit to decimal and add it to the result
            result += digit * factor;

            // Multiply the factor by 10 for the next BCD digit
            factor *= 10;

            // Shift the BCD value to the right by 4 bits to remove the processed digit
            bcdValue >>= 4;
        }

        return result;
    }

    /// <summary>
    /// Takes a decimal value and turns it into a BCD value.
    /// It does not check for correctnes of the decimal value!!
    /// 
    /// This function was written with Chat-GPT.
    /// Notes:
    /// </summary>
    public static int DecimalToBcd(int decimalValue)
    {
        int result = 0;
        int factor = 1;

        // Iterate over the decimal digits from right to left
        while (decimalValue != 0)
        {
            // Extract the rightmost decimal digit
            int digit = decimalValue % 10;

            // Convert the decimal digit to BCD and add it to the result
            result += digit * factor;

            // Multiply the factor by 16 for the next BCD digit
            factor *= 16;

            // Shift the decimal value to the right by 1 digit to remove the processed digit
            decimalValue /= 10;
        }

        return result;
    }
}
