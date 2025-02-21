using LAMP.Utilities;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LAMP.Classes
{
    public struct TransitionOpcode
    {
        [JsonConstructor]
        public TransitionOpcode(string OpcodeString)
        {
            this.OpcodeString = OpcodeString;

            //get nybble data
            string[] templates = OpcodeInterpreter.GetDataTemplates(OpcodeString);
            NybbleIndices = new int?[templates.Length];
            ParameterLength = new int[templates.Length];
            PredefinedValue = new int?[templates.Length];
            ParameterListNames = new string[templates.Length];

            for (int i = 0; i < templates.Length; i++)
            {
                int? ind = null;
                int length = 1;
                int? value = null;
                string listName = "";

                string template = templates[i];

                if (template != "")
                {
                    MatchCollection c = Regex.Matches(Regex.Matches(template, @"\[(.*?)\]")[0].Value, @"[0-9]+");
                    ind = int.Parse(c[0].Value); //get the start nybble
                    if (c.Count > 1) //This means theres a range of nybbles
                    {
                        length = int.Parse(c[1].Value) - int.Parse(c[0].Value) + 1; //get the amount of nybbles
                    }

                    //Interpreting the predefined value
                    /*
                     * Predefined values can be used to
                     * 1. Set a predefined value, duh
                     *    Example: (0xA5)
                     * 2. Reference a Global List with INamedResource content. This will turn the
                     *    Input into a Combobox with the list content. The selected index is the value
                     *    of the parameter.
                     *    Example: (Transitions)
                     * 3. Set options. Options are a list of strings which get interpreted as a Combobox
                     *    with the option content. The selected index is the value.
                     *    Example: (Sprites; Backgrounds; Data)
                     */
                    string val = Regex.Match(Regex.Match(template, @"\((.*?)\)").Value, @"[^()].*[^()]").Value;

                    //Check first if input string references a INamedResource List
                    string[] options = val.Split(';');
                    if (typeof(Globals).GetField(val) != null || typeof(Globals).GetProperty(val) != null || options.Length > 1) 
                    {
                        listName = val;
                        val = "";
                    }

                    if (val != "") value = Format.StringToInt(val);
                }

                NybbleIndices[i] = ind;
                ParameterLength[i] = length;
                PredefinedValue[i] = value;
                ParameterListNames[i] = listName;
            }
        }

        /// <summary>
        /// The full string that represents the opcode
        /// </summary>
        public string OpcodeString { get; private set; }

        /// <summary>
        /// The index that represents the opcode
        /// </summary>
        [JsonIgnore]
        public byte OpcodeIndex => OpcodeInterpreter.GetOpcodeIndex(OpcodeString);

        /// <summary>
        /// An array which stores the title of the transition and the description of each parameter
        /// </summary>
        [JsonIgnore]
        public string[] Description => OpcodeInterpreter.GetOpCodeDescription(OpcodeString);

        /// <summary>
        /// An array which stores the start nybble for each parameter. Set to null if the parameter does not have any modifyable data
        /// </summary>
        [JsonIgnore]
        public int?[] NybbleIndices { get; set; }

        /// <summary>
        /// An array which stores the length of each parameter in nybbles. Set to 1 if the parameter does not have any modifyable data
        /// </summary>
        [JsonIgnore]
        public int[] ParameterLength { get; set; }

        /// <summary>
        /// An array of the predefined value of a parameter. Set to null if a parameter does not have a predefined value
        /// </summary>
        [JsonIgnore]
        public int?[] PredefinedValue { get; set; }

        /// <summary>
        /// An array of the Names of the lists that should be used in the dropdown menu
        /// </summary>
        [JsonIgnore]
        public string[] ParameterListNames { get; set; } = null;

        /// <summary>
        /// The number of bytes that make up the opcode
        /// </summary>
        [JsonIgnore]
        public int OpcodeLength => OpcodeInterpreter.GetOpcodeLength(OpcodeString);

        /// <summary>
        /// Whether the opcode has more than 
        /// </summary>
        [JsonIgnore]
        public bool isExpandable
        {
            get => Description.Length > 1;
        }
    }
}
