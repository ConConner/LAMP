using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LAMP.Classes;

public class TweakValue
{
    /// <summary>
    /// The type of the <see cref="TweakValue"/>. Defines how the Value(s) will be interpreted.
    /// </summary>
    public TweakDataTypes Type { get; set; }

    /// <summary>
    /// The value for the tweak. Cannot be used if multiple values are supplied
    /// </summary>
    [JsonIgnore]
    public byte Value => Values.Count >= 1 ? throw new InvalidOperationException("There is more than 1 value stored for this object") : Values[0];
    public List<byte> Values { get; set; } = new List<byte>();
}

public enum TweakDataTypes
{
    Byte,
    Short,
    Pointer,
    Flag
}