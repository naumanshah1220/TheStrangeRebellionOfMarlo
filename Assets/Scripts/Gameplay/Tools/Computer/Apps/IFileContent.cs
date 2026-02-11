/// <summary>
/// Interface for file content prefabs that can be initialized with file data
/// </summary>
public interface IFileContent
{
    /// <summary>
    /// Initialize the content with file data
    /// </summary>
    /// <param name="file">The file data to display</param>
    void Initialize(DiscFile file);
} 