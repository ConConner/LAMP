namespace LAMP.Interfaces;

/// <summary>
/// This interface requires an implementation where a given File can be used to do an operation.
/// </summary>
public interface IFileOperation
{
    /// <summary>
    /// Path to the file
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// Operation to be 
    /// </summary>
    /// <returns>true if the operation was successful, false if not</returns>
    public bool DoOperation();
}
