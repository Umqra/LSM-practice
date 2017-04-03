using System.Collections.Generic;
using System.IO.Abstractions;

namespace DataLayer.Utilities
{
    public interface IFileTracker
    {
        DirectoryInfoBase WorkingDirectory { get; }
        IEnumerable<FileInfoBase> Files { get; }
        FileInfoBase CreateNewFile();
    }
}