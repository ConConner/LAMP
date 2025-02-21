using System;
using System.Drawing;
using System.Text.Json.Serialization;

namespace LAMP.Classes.M2_Data.GBC;

//A datastructure that represents a 15-bit GBC RGB Color Value
public struct GBColor
{
    public GBColor(byte R, byte G, byte B)
    {
        this.R = R;
        this.G = G;
        this.B = B;
    }

    [JsonConstructor]
    public GBColor(ushort Value)
    {
        this.Value = Value;
    }

    /// <summary>
    /// The RGB-Value of the <see cref="GBColor"/>
    /// </summary>
    public ushort Value { get; private set; } = 0x0;

    /// <summary>
    /// The red component of the color. Maximum value of 0x1F (DEC 31)
    /// </summary>
    [JsonIgnore]
    public byte R 
    { 
        get
        {
            return (byte)(Value >> 0 & 0b11111);
        }
        set
        {
            if (value > 0x1F) throw new ArgumentOutOfRangeException("Maximum allowed value is 0x1F (DEC 31)");
            this.Value = (ushort)((B << 10) | (G << 5) | value);
        }
    }

    /// <summary>
    /// The green component of the color. Maximum value of 0x1F (DEC 31)
    /// </summary>
    [JsonIgnore]
    public byte G
    {
        get
        {
            return (byte)(Value >> 5 & 0b11111);
        }
        set
        {
            if (value > 0x1F) throw new ArgumentOutOfRangeException("Maximum allowed value is 0x1F (DEC 31)");
            this.Value = (ushort)((B << 10) | (value << 5) | R);
        }
    }

    /// <summary>
    /// The blue component of the color. Maximum value of 0x1F (DEC 31)
    /// </summary>
    [JsonIgnore]
    public byte B
    {
        get
        {
            return (byte)(Value >> 10 & 0b11111);
        }
        set
        {
            if (value > 0x1F) throw new ArgumentOutOfRangeException("Maximum allowed value is 0x1F (DEC 31)");
            this.Value = (ushort)((value << 10) | (G << 5) | R);
        }
    }

    /// <summary>
    /// Returns a 15-bit GBC Color from a 24-bit PC RGB Color
    /// </summary>
    /// <returns></returns>
    public static GBColor FromRGB(byte R, byte G, byte B)
    {
        return new GBColor((byte)(R / 8), (byte)(G / 8), (byte)(B / 8));
    }

    public static GBColor FromRGB(Color color)
    {
        return FromRGB(color.R, color.G, color.B);
    }

    /// <summary>
    /// Returns a 24-bit PC RGB Color from a 15-bit GBC Color
    /// </summary>
    public static Color ToRGB(byte R, byte G, byte B)
    {
        return Color.FromArgb(R * 8, G * 8, B * 8);
    }
    /// <summary>
    /// Returns a 24-bit PC RGB Color from the current 15-bit GBC Color
    /// </summary>
    public Color ToRGB()
    {
        return ToRGB(R, G, B);
    }
}
