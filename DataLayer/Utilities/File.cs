using System.IO;

namespace DataLayer.Utilities
{
    public class File : IFile
    {
        private string path;
        public File(string path)
        {
            this.path = path;
        }

        public Stream GetStream(FileMode mode, FileAccess accessMode)
        {
            return System.IO.File.Open(path, mode, accessMode);
        }
    }
}