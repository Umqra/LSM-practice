using System.IO;

namespace DataLayer.Utilities
{
    public interface IFile
    {
        string Path { get; set; }
        Stream GetStream(FileMode mode, FileAccess accessMode);
    }
}