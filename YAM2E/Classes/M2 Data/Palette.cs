using System.Collections.Generic;
using System.Text.Json.Serialization;
using LAMP.Classes.M2_Data.GBC;
using LAMP.Interfaces;

namespace LAMP.Classes.M2_Data;

/// <summary>
/// A data structure to resemble a list of GBC colors, 4 Colors per index
/// </summary>
public class Palette : INamedResource
{
    [JsonConstructor]
    public Palette()
    {
    }

    /// <summary>
    /// Name of the Palette
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// List of Color lists. Each entry has a length of 4 Colors
    /// </summary>
    public List<GBColor[]> Colors { get; set; } = new();
}