using System;

namespace LAMP.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class SettingAttribute : Attribute
{
    public SettingAttribute() { }
}
