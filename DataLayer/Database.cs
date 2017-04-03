using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using DataLayer.DataModel;
using DataLayer.DiskTable;
using DataLayer.MemoryCache;
using DataLayer.OperationLog;
using DataLayer.Utilities;

namespace DataLayer
{
    //TODO: singletone class?
    public class Database : IDataStorage, IDisposable
    {
        private readonly DirectoryInfoBase workingDirectory;
        private readonly DiskTableManager diskTableManager;
        private readonly CacheManager cacheManager;

        public Database(DirectoryInfoBase workingDirectory, IDumpCriteria dumpCriteria, IFileInfoFactory fileFactory)
        {
            this.workingDirectory = workingDirectory;
            diskTableManager = new DiskTableManager(new DiskTableManagerConfiguraiton
            {
                DiskTablesTracker = new FileTracker("sstable-{0}.txt", workingDirectory, fileFactory)
            });
            cacheManager = new CacheManager(new CacheManagerConfiguration
            {
                DumpCriteria = dumpCriteria,
                OperationLogsTracker = new FileTracker("log-{0}.txt", workingDirectory, fileFactory),
                Repairer = new OperationLogRepairer(),
                DiskTableManager = diskTableManager
            });
        }

        public int Size { get; }
        public void Delete(string key)
        {
            cacheManager.Delete(key);
        }

        public Item Get(string key)
        {
            return cacheManager.Get(key) ?? diskTableManager.Get(key);
        }

        public IEnumerable<Item> GetAllItems()
        {
            //TODO: fix interfaces for avoiding such stubss
            throw new NotImplementedException();
        }

        public void Add(Item item)
        {
            cacheManager.Add(item);
        }

        public void Dispose()
        {
            cacheManager?.Dispose();
            diskTableManager?.Dispose();;
        }
    }
}