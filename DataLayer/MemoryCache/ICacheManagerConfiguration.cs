using DataLayer.DiskTable;
using DataLayer.OperationLog;
using DataLayer.Utilities;

namespace DataLayer.MemoryCache
{
    public interface ICacheManagerConfiguration
    {
        IFileTracker OperationLogsTracker { get; }
        IOperationLogRepairer Repairer { get; }
        IDumpCriteria DumpCriteria { get; }
        IDiskTableManager DiskTableManager { get; }
    }
}