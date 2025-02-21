using System;
using System.Text.RegularExpressions;
using LAMP.Utilities;

namespace LAMP
{
    public static class OpcodeInterpreter
    {
        /// <summary>
        /// Takes an opcode string and returns all descriptive strings in it
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string[] GetOpCodeDescription(string input)
        {
            //start extracting elements
            string[] parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = Regex.Replace(parts[i], @"\[(.*?)\]|\((.*?)\)", ""); //removes data templates [...](...) or [...]
                parts[i] = parts[i].Trim(); //removes spaces before and after string
            }
            return parts;
        }

        /// <summary>
        /// Takes an opcode string and returns all the data templates from it
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string[] GetDataTemplates(string input)
        {
            //start extracting elements
            string[] parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = Regex.Match(parts[i], @"\[(.*?)\](\((.*?)\))?").Value; //gets data templates [...](...) or [...]
                parts[i] = parts[i].Trim(); //removes spaces before and after string
            }
            return parts;
        }

        /// <summary>
        /// Gets the amount of Bytes used by the opcode
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static int GetOpcodeLength(string input)
        {
            int nybbleCount = 0;

            //start extracting elements
            string[] parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = Regex.Match(parts[i], @"\[(.*?)\]").Value; //gets data templates [...]
            }
            foreach(string s in parts) //checking if its a range
            {
                if (s == "") continue;
                MatchCollection c = Regex.Matches(s, @"[0-9]+");
                if (c.Count > 1) //This means theres a range of nybbles
                {
                    //calculating the amount of nybbles
                    nybbleCount += int.Parse(c[1].Value) - int.Parse(c[0].Value) + 1;
                }
                else nybbleCount++; //otherwise there is only one nybble thats being accessed
            }

            if (nybbleCount == 0) return 0;
            return nybbleCount / 2 + nybbleCount % 2;   //calculates how many bytes get started through the nybbles
        }

        /// <summary>
        /// Returns the byte which represents the opcode in the data
        /// </summary>
        public static byte GetOpcodeIndex(string input)
        {
            string[] data = GetDataTemplates(input);
            if (data.Length == 0 || data[0] == "") throw new Exception("No Opcode index has been defined");

            string indexstring = Regex.Match(data[0], @"\((.*?)\)").Value; //gets the prewritten data in the first line (...)
            if (indexstring == "") throw new Exception("No Opcode index has been defined");

            return (byte)Format.StringToInt(Regex.Match(indexstring, @"([0-9]|[A-F])+").Value);
        }

        /// <summary>
        /// Returns if the opcode has any parameters which can be modified
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool HasParameters(string input)
        {
            return false;
        }
    }
}
