using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.DataModel;
using DataLayer.MemoryCache;
using DataLayer.Utilities;

namespace DataLayer.DiskTable
{
    public class DiskTableManagerConfiguraiton
    {
        public DirectoryInfo WorkingDirectory { get; set; }
        public Func<string> UniqueNameGenerator { get; set; }
    }

    public class QueueEntry<T>
    {
        public T Value { get; set; }
        public bool StartMerging { get; set; }

        public QueueEntry(T value)
        {
            Value = value;
            StartMerging = false;
        }
    }

    public class DiskTablesQueue : IEnumerable<DiskTable>
    {
        private readonly object poolLock = new object();
        private readonly SynchronizedCollection<QueueEntry<DiskTable>> diskTables;

        public DiskTablesQueue()
        {
            diskTables = new SynchronizedCollection<QueueEntry<DiskTable>>();
        }

        public void Push(DiskTable table)
        {
            //TODO: Poor performance because of linear insert?
            diskTables.Insert(0, new QueueEntry<DiskTable>(table));
        }

        public void Pop()
        {
            lock (poolLock)
                diskTables.RemoveAt(diskTables.Count - 1);
        }

        private int GetLastNotMergedId()
        {
            lock (diskTables)
            {
                for (int i = diskTables.Count - 1; i >= 0; i--)
                    if (!diskTables[i].StartMerging)
                        return i;
                return -1;
            }
        }

        public DiskTable TryMerge()
        {
            QueueEntry<DiskTable> firstTable, secondTable;
            lock (diskTables)
            {
                var notMerged = GetLastNotMergedId();
                if (notMerged <= 0)
                    return null;
                diskTables[notMerged].StartMerging = diskTables[notMerged - 1].StartMerging = true;
                firstTable = diskTables[notMerged];
                secondTable = diskTables[notMerged - 1];
            }
            throw new NotImplementedException();
            //var merged = DiskTableManager.MergeDiskTables(firstTable.Value, secondTable.Value);
            //diskTables.Remove(firstTable);
            //diskTables.Remove(secondTable);
            //return merged;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<DiskTable> GetEnumerator()
        {
            var tables = new List<DiskTable>();
            lock (diskTables)
            {
                for (int i = diskTables.Count - 1; i >= 0; i--)
                    tables.Add(diskTables[i].Value);
            }
            return tables.GetEnumerator();
        }
    }
    public class DiskTableManager : IDiskTableManager
    {
        private readonly DiskTableManagerConfiguraiton configuration;
        private const int DefaultIndexSpanSize = 100;
        private readonly SynchronizedCollection<DiskTablesQueue> diskTableLevels;
        private readonly SynchronizedCollection<Cache> pendingCaches;

        public DiskTableManager(DiskTableManagerConfiguraiton configuration)
        {
            this.configuration = configuration;
            diskTableLevels = new SynchronizedCollection<DiskTablesQueue>();
            pendingCaches = new SynchronizedCollection<Cache>();
        }
        public Item Get(string key)
        {
            foreach (var cache in pendingCaches)
            {
                var cacheValue = cache.Get(key);
                if (cacheValue != null)
                    return cacheValue;
            }
            foreach (var level in diskTableLevels)
            {
                foreach (var diskTable in level)
                {
                    var tableValue = diskTable.Get(key);
                    if (tableValue != null)
                        return tableValue;
                }
            }
            return null;
        }

        public IEnumerable<Item> GetAllItems()
        {
            throw new NotImplementedException();
        }

        private void AddDiskTable(DiskTable diskTable, int level)
        {
            lock (diskTableLevels)
            {
                while (diskTableLevels.Count <= level)
                    diskTableLevels.Add(new DiskTablesQueue());
            }
            diskTableLevels[level].Push(diskTable);
            FixLevels();
        }

        private void FixLevels()
        {
            var count = diskTableLevels.Count;
            for (var i = 0; i < count; i++)
            {
                var level = i;
                Task.Run(() =>
                {
                    var merged = diskTableLevels[level].TryMerge();
                    AddDiskTable(merged, level + 1);
                });
            }
        }

        public void DumpCache(Cache cache, Action cleanupAction)
        {
            var diskTableConfig = CreateDiskTableConfiguration();
            pendingCaches.Add(cache);
            Task.Run(async () => await DiskTable.DumpCache(diskTableConfig, cache)).ContinueWith(t =>
            {
                AddDiskTable(t.Result, 0);
                //TODO: removing by reference is TOO slow
                pendingCaches.Remove(cache);
                cleanupAction();
            });
        }

        private DiskTableConfiguration CreateDiskTableConfiguration()
        {
            var path = Path.Combine(configuration.WorkingDirectory.FullName, configuration.UniqueNameGenerator());
            return new DiskTableConfiguration
            {
                IndexSpanSize =  DefaultIndexSpanSize,
                Serializer = new ItemSerializer(),
                TableFile = new FileInfo(path)
            };
        }

        public DiskTable MergeDiskTables(DiskTable first, DiskTable second)
        {
            var configuration = CreateDiskTableConfiguration();
            throw new NotImplementedException();    
        }
    }
}
