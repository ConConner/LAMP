using LAMP.Interfaces;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LAMP.Classes;

public class Tweak : IIncludableResource
{
    /// <summary>
    /// Name of the tweak. Only used in LAMP
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Optional descritpion for the tweak
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// The location of the tweak. Can either be a pointer of a label from the disassembly
    /// </summary>
    public string Location { get; set; }

    /// <summary>
    /// List of parameters and values that gets written at the offset
    /// </summary>
    public List<TweakValue> Parameters { get; set; }

    public bool Include { get; set; } = true;

    [JsonConstructor]
    public Tweak() { }

    [JsonIgnore]
    public Pointer? LocationPointer
    {
        get
        {
            if (!Rom.ContainsSymbol(Location)) return null;
            return Rom.OffsetOf(Location);
        }
    }
}