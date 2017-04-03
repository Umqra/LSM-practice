using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using DataLayer.DataModel;
using DataLayer.OperationLog;
using DataLayer.OperationLog.Operations;

namespace DataLayer.MemoryCache
{
    public class CacheManager : IDataStorage, IDisposable
    {
        private FileInfoBase cacheLogFile;
        private readonly ICacheManagerConfiguration configuration;
        private Cache currentCache;

        public int Size => currentCache?.Size ?? 0;

        public CacheManager(ICacheManagerConfiguration configuration)
        {
            this.configuration = configuration;
            cacheLogFile = GetValidCache();
            currentCache = InitializeCacheWithLog(cacheLogFile);
        }

        public void Add(Item item)
        {
            lock (currentCache)
            {
                currentCache.Add(item);
            }
            DumpIfNeeded();
        }

        public void Delete(string key)
        {
            lock (currentCache)
            {
                currentCache.Delete(key);
            }
            DumpIfNeeded();
        }

        public Item Get(string key)
        {
            lock (currentCache)
            {
                return currentCache.Get(key);
            }
        }

        public IEnumerable<Item> GetAllItems()
        {
            lock (currentCache)
            {
                return currentCache.GetAllItems();
            }
        }

        public void Dispose()
        {
            lock (currentCache)
            {
                currentCache?.Dispose();
            }
        }

        private void DumpCache()
        {
            currentCache.PrepareToDump();
            currentCache.Dispose();
            var oldCache = currentCache;
            var oldCacheFile = cacheLogFile;
            cacheLogFile = configuration.OperationLogsTracker.CreateNewFile();
            currentCache = InitializeCacheWithLog(cacheLogFile);

            configuration.DiskTableManager.DumpCache(oldCache, () => oldCacheFile.Delete());
        }

        private void DumpIfNeeded()
        {
            lock (currentCache)
            {
                if (configuration.DumpCriteria.ShouldDump(currentCache))
                    DumpCache();
            }
        }

        private Cache InitializeCacheWithLog(FileInfoBase logFile)
        {
            var dataStorage = new DataStorage();
            using (
                var reader = new OperationLogReader(logFile.Open(FileMode.OpenOrCreate, FileAccess.Read),
                    new OperationSerializer()))
            {
                IOperation operation;
                while (reader.Read(out operation))
                    operation.Apply(dataStorage);
            }

            var cacheLogWriter = new OperationLogWriter(
                logFile.Open(FileMode.Append, FileAccess.Write),
                new OperationSerializer());
            return new Cache(cacheLogWriter, dataStorage);
        }

        private FileInfoBase GetValidCache()
        {
            var candidateForRestore = new List<FileInfoBase>();
            foreach (var logFile in configuration.OperationLogsTracker.Files)
            {
                //TODO: read EACH log file TWICE!
                var operation = ReadOperationLog(logFile).ToList();
                if (operation.Any() && operation.Last() is DumpOperation)
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