namespace LAMP.Interfaces;

public interface IIncludableResource : INamedResource
{
    public bool Include { get; set; }
}
