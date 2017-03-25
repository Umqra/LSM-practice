using System.IO;

namespace DataLayer.Utilities
{
    public interface IFileData
    {
        string Path { get; set; }
        Stream Open(FileMode mode, FileAccess accessMode);
    }
}