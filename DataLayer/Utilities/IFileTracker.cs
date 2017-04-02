using System.Collections.Generic;

namespace DataLayer.Utilities
{
    public interface IFileTracker
    {
        IEnumerable<FileData> Files { get; }
        FileData CreateNewFile();
    }
}