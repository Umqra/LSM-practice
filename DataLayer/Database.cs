using System;
using System.Collections.Generic;
using System.IO;
using DataLayer.DataModel;
using DataLayer.DiskTable;
using DataLayer.MemoryCache;
using DataLayer.OperationLog;
using DataLayer.Utilities;

namespace DataLayer
{
    // управляет memoryhash, disk tables, operationLog и управляет всеми запросами
    // а так же управляет мерджингом
    public class Database : IDataStorage
    {
        private readonly DiskTableManager diskTableManager;
        private readonly CacheManager cacheManager;

        public Database(string workingDirectory)
        {
            diskTableManager = new DiskTableManager(new DiskTableManagerConfiguraiton
            {
                DiskTablesTracker = new FileTracker("sstable-{0}.txt", new DirectoryInfo(workingDirectory))
            });
            cacheManager = new CacheManager(new CacheManagerConfiguration
            {
                DumpCriteria  = new SizeDumpCriteria(10),
                OperationLogsTracker = new FileTracker("log-{0}.txt", new DirectoryInfo(workingDirectory)),
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
    }
}