using System.IO;

namespace DataLayer.Utilities
{
    public class FileData : IFileData
    {
        public string Path { get; set; }
        public FileData(string path)
        {
            Path = path;
        }

        public Stream GetStream(FileMode mode, FileAccess accessMode)
        {
            return File.Open(Path, mode, accessMode);
        }
    }
}