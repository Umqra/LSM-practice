using System.IO.Abstractions;
using DataLayer.DataModel;
using DataLayer.Utilities;

namespace DataLayer.DiskTable
{
    public class DiskTableConfiguration
    {
        public FileInfoBase TableFile { get; set; }
        public int IndexSpanSize { get; set; }
        public IItemSerializer Serializer { get; set; }
    }
}