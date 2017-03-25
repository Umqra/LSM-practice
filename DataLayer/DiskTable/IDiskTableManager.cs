using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.MemoryCopy;

namespace DataLayer.DiskTable
{
    public interface IDiskTableManager : IDataReader
    {
        void DumpCache(Cache cache, Action cleanupAction);
    }
}
