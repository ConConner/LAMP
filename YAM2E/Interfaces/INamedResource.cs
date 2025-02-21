namespace LAMP.Interfaces;

/// <summary>
/// An Interface, whichs goal is to standardize the resources, extracted from M2, in LAMP
/// </summary>
public interface INamedResource
{
    public string Name { get; set; }
}
