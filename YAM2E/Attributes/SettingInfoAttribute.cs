using System;

namespace LAMP.Attributes;

/// <summary>
/// Attribute declaring text metadata for display on the UI
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class SettingInfoAttribute : Attribute
{ 
    /// <summary>
    /// Optionally can also be split into subcatogories with a comma
    /// </summary>
    public string Category { get; }
    public string DisplayName { get; }
    public string Description { get; }

    public SettingInfoAttribute(string displayName = null, string description = null, string category = null) 
    {
        DisplayName = displayName;
        Description = description;
        Category = category;
    }
}
