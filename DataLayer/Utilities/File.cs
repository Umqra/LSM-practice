using System.IO;

namespace DataLayer.Utilities
{
    public class File : IFile
    {
        public string Path { get; set; }
        public File(string path)
        {
            Path = path;
        }

        public Stream GetStream(FileMode mode, FileAccess accessMode)
        {
            return System.IO.File.Open(Path, mode, accessMode);
        }
    }
}