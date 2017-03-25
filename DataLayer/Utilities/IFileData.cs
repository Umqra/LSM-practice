using System.IO;

namespace DataLayer.Utilities
{
    public interface IFileData
    {
        string Path { get; set; }
        Stream GetStream(FileMode mode, FileAccess accessMode);
    }
}