using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataLayer.DataModel;
using DataLayer.DiskTable;
using DataLayer.OperationLog;
using DataLayer.OperationLog.Operations;
using DataLayer.Utilities;

namespace DataLayer.MemoryCopy
{
    public class CacheManagerConfiguration
    {
        public DirectoryInfo WorkingDirectory { get; set; }
        public Func<FileInfo, bool> OperationLogFilter { get; set; }
        public IOperationLogRepairer Repairer { get; set; }
        public Func<string> UniqueNameGenerator { get; set; }
        public IDumpCriteria DumpCriteria { get; set; }
        public IDiskTableManager DiskTableManager { get; set; }
    }

    public interface IDumpCriteria
    {
        bool ShouldDump(Cache cache);
    }

    public class CacheManager : IDataStorage, IDisposable
    {
        private readonly CacheManagerConfiguration configuration;
        private readonly IFileData cacheLogFile;
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
            set { currentCache = value; }
        }

        private void DumpCache()
        {
            currentCache.PrepareToDump();
            currentCache.Dispose();
            configuration.DiskTableManager.DumpCache(currentCache, () => File.Delete(cacheLogFile.Path));
            currentCache = InitializeCacheWithLog(CreateNewLogFile());
        }

        public int Size => CurrentCache?.Size ?? 0;

        public CacheManager(CacheManagerConfiguration configuration)
        {
            this.configuration = configuration;
            cacheLogFile = GetValidCache();
            CurrentCache = InitializeCacheWithLog(cacheLogFile);
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

        private FileData CreateNewLogFile()
        {
            var filename = configuration.UniqueNameGenerator();
            return new FileData(Path.Combine(configuration.WorkingDirectory.FullName, filename));
        }

        private Cache InitializeCacheWithLog(IFileData logFile)
        {
            var cacheLogWriter = new OperationLogWriter(
                logFile.Open(FileMode.Append, FileAccess.Write),
                new OperationSerializer());
            return new Cache(cacheLogWriter, new DataStorage());
        }

        private IFileData GetValidCache()
        {
            var candidateForRestore = new List<FileData>();
            foreach (var file in configuration.WorkingDirectory.GetFiles().Where(configuration.OperationLogFilter))
            {
                var logFile = new FileData(file.FullName);
                //TODO: read EACH log file TWICE!
                var operation = ReadOperationLog(logFile).ToList();
                if (operation.Last() is DumpOperation)
                    continue;
                candidateForRestore.Add(logFile);
            }
            if (candidateForRestore.Count == 0)
                return CreateNewLogFile();
            //TODO: Notification if more than one candidate
            return candidateForRestore.First();
        }

        private IEnumerable<IOperation> ReadOperationLog(IFileData logFile)
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