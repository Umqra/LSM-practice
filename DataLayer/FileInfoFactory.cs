using System.IO;
using System.IO.Abstractions;

namespace DataLayer
{
    public class FileInfoFactory : IFileInfoFactory
    {
        public FileInfoBase FromFileName(string fileName)
        {
            return new FileInfo(fileName);
        }
    }
}