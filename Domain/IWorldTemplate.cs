namespace AutoWorldLoader
{
    /// <summary>
    /// Contract for a world template implementation.
    /// Used as a value in <see cref="WorldTemplateRegistry"/>'s dictionary.
    /// </summary>
    public interface IWorldTemplate
    {
        /// <summary>Folder name under Templates\ (also the default target name in Saves).</summary>
        string FolderName { get; }
    }
}
