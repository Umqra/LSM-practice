using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.MemoryCache;

namespace DataLayer.DiskTable
{
    public interface IDiskTableManager : IDataReader, IDisposable
    {
        void DumpCache(Cache cache, Action cleanupAction);
    }
}
