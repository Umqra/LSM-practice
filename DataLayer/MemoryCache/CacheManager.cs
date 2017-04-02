using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using DataLayer.DataModel;
using DataLayer.OperationLog;
using DataLayer.OperationLog.Operations;

namespace DataLayer.MemoryCache
{
    public class CacheManager : IDataStorage, IDisposable
    {
        private readonly ICacheManagerConfiguration configuration;
        private readonly FileInfoBase cacheLogFile;
        private Cache currentCache;

        private Cache CurrentCache
        {
            get
            {
                lock (cacheLogFile)
                {
                    if (configuration.DumpCriteria.ShouldDump(currentCache))
                        DumpCache();
                    return currentCache;
                }
            }
        }

        private void DumpCache()
        {
            currentCache.PrepareToDump();
            currentCache.Dispose();
            configuration.DiskTableManager.DumpCache(currentCache, () => cacheLogFile.Delete());
            currentCache = InitializeCacheWithLog(configuration.OperationLogsTracker.CreateNewFile());
        }

        public int Size => CurrentCache?.Size ?? 0;

        public CacheManager(ICacheManagerConfiguration configuration)
        {
            this.configuration = configuration;
            cacheLogFile = GetValidCache();
            currentCache = InitializeCacheWithLog(cacheLogFile);
        }

        public void Add(Item item)
        {
            CurrentCache.Add(item);
        }

        public void Delete(string key)
        {
            CurrentCache.Delete(key);
        }

        public Item Get(string key)
        {
            return CurrentCache.Get(key);
        }

        public IEnumerable<Item> GetAllItems()
        {
            return CurrentCache.GetAllItems();
        }

        public void Dispose()
        {
            CurrentCache?.Dispose();
        }

        private Cache InitializeCacheWithLog(FileInfoBase logFile)
        {
            var cacheLogWriter = new OperationLogWriter(
                logFile.Open(FileMode.Append, FileAccess.Write),
                new OperationSerializer());
            return new Cache(cacheLogWriter, new DataStorage());
        }

        private FileInfoBase GetValidCache()
        {
            var candidateForRestore = new List<FileInfoBase>();
            foreach (var logFile in configuration.OperationLogsTracker.Files)
            {
                //TODO: read EACH log file TWICE!
                var operation = ReadOperationLog(logFile).ToList();
                if (operation.Last() is DumpOperation)
                    continue;
                candidateForRestore.Add(logFile);
            }
            if (candidateForRestore.Count == 0)
                return configuration.OperationLogsTracker.CreateNewFile();
            //TODO: Notification if more than one candidate
            return candidateForRestore.First();
        }

        private IEnumerable<IOperation> ReadOperationLog(FileInfoBase logFile)
        {
            configuration.Repairer.RepairLog(logFile);
            using (var reader = new OperationLogReader(
                logFile.Open(FileMode.OpenOrCreate, FileAccess.Read),
                new OperationSerializer()))
            {
                IOperation operation;
                while (reader.Read(out operation))
                    yield return operation;
            }
        }
    }
}