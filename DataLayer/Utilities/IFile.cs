using System.IO;

namespace DataLayer.Utilities
{
    public interface IFile
    {
        Stream GetStream(FileMode mode, FileAccess accessMode);
    }
}