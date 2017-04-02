using DataLayer.DiskTable;
using DataLayer.OperationLog;
using DataLayer.Utilities;

namespace DataLayer.MemoryCache
{
    public class CacheManagerConfiguration : ICacheManagerConfiguration
    {
        public IFileTracker OperationLogsTracker { get; set; }
        public IOperationLogRepairer Repairer { get; set; }
        public IDumpCriteria DumpCriteria { get; set; }
        public IDiskTableManager DiskTableManager { get; set; }
    }
}