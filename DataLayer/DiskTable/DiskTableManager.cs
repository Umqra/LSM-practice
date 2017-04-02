using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C5;
using DataLayer.DataModel;
using DataLayer.MemoryCache;
using DataLayer.Utilities;

namespace DataLayer.DiskTable
{
    public interface IDiskTableManagerConfiguration
    {
        IFileTracker DiskTablesTracker { get; }
    }
    public class DiskTableManagerConfiguraiton : IDiskTableManagerConfiguration
    {
        public IFileTracker DiskTablesTracker { get; set; }
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
        private readonly DiskTableManager diskTableManager;
        private readonly SynchronizedCollection<QueueEntry<DiskTable>> diskTables;

        //TODO: pass IDiskTableMerger instead of DiskTableManager?
        public DiskTablesQueue(DiskTableManager diskTableManager)
        {
            this.diskTableManager = diskTableManager;
            diskTables = new SynchronizedCollection<QueueEntry<DiskTable>>();
        }

        public void Push(DiskTable table)
        {
            //TODO: Poor performance because of linear insert?
            diskTables.Insert(0, new QueueEntry<DiskTable>(table));
        }

        public void Remove(DiskTable diskTable)
        {
            //TODO: very SLOW remove!
            lock (diskTables)
            {
                var target = diskTables.FirstOrDefault(table => table.Value.Equals(diskTable));
                if (target == null)
                    return;
                diskTables.Remove(target);
            }
        }

        private IEnumerable<QueueEntry<DiskTable>> GetRecentNotMerged()
        {
            lock (diskTables)
            {
                for (int i = diskTables.Count - 1; i >= 0; i--)
                    if (!diskTables[i].StartMerging)
                        yield return diskTables[i];
            }
        }

        public async Task<DiskTablesMergeInfo> TryMerge()
        {
            QueueEntry<DiskTable>[] parents;
            lock (diskTables)
            {
                parents = GetRecentNotMerged().Take(2).ToArray();
                if (parents.Length < 2)
                    return null;
                parents[0].StartMerging = parents[1].StartMerging = true;
            }
            
            var merged = await diskTableManager.MergeDiskTables(parents[0].Value, parents[1].Value);
            return new DiskTablesMergeInfo(merged, parents.Select(parent => parent.Value));
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

    public class DiskTablesMergeInfo
    {
        public  readonly DiskTable Merged;
        public readonly List<DiskTable> Parents;

        public DiskTablesMergeInfo(DiskTable merged, IEnumerable<DiskTable> parents)
        {
            Merged = merged;
            Parents = parents.ToList();
        }
    }

    public class DiskTableManager : IDiskTableManager
    {
        private readonly DiskTableManagerConfiguraiton configuration;
        private const int DefaultIndexSpanSize = 100;
        private readonly SynchronizedCollection<DiskTablesQueue> diskTableLevels;
        private readonly SynchronizedCollection<Cache> dumpingCachesQueue;

        public DiskTableManager(DiskTableManagerConfiguraiton configuration)
        {
            this.configuration = configuration;
            diskTableLevels = new SynchronizedCollection<DiskTablesQueue>();
            dumpingCachesQueue = new SynchronizedCollection<Cache>();
            InitializeDiskTables();
        }

        private void InitializeDiskTables()
        {
            foreach (var diskTableFile in configuration.DiskTablesTracker.Files)
            {
                int diskTableLevel;
                using (var stream = diskTableFile.OpenRead())
                using (var binaryReader = new BinaryReader(stream))
                    diskTableLevel = binaryReader.ReadInt32();
                //TODO: create new disk table
            }
        }

        public Item Get(string key)
        {
            foreach (var cache in dumpingCachesQueue)
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
                    diskTableLevels.Add(new DiskTablesQueue(this));
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
                Task.Run(async () =>
                {
                    var mergeResult = await diskTableLevels[level].TryMerge();
                    //TODO: specific merge result instead of null for this case
                    if (mergeResult == null) return;
                    AddDiskTable(mergeResult.Merged, level + 1);

                    //TODO: parent can handle query right now(from concurrent thread) and we may interrupt it
                    foreach (var parent in mergeResult.Parents)
                        diskTableLevels[level].Remove(parent);
                });
            }
        }

        public void DumpCache(Cache cache, Action cleanupAction)
        {
            var diskTableConfig = CreateDiskTableConfiguration();
            dumpingCachesQueue.Add(cache);
            Task.Run(async () => await DiskTable.DumpCache(diskTableConfig, cache)).ContinueWith(t =>
            {
                AddDiskTable(t.Result, 0);
                //TODO: removing by reference is TOO slow
                dumpingCachesQueue.Remove(cache);
                cleanupAction();
            });
        }

        private DiskTableConfiguration CreateDiskTableConfiguration()
        {
            var file = configuration.DiskTablesTracker.CreateNewFile();
            return new DiskTableConfiguration
            {
                IndexSpanSize =  DefaultIndexSpanSize,
                Serializer = new ItemSerializer(),
                TableFile = file,
            };
        }

        public async Task<DiskTable> MergeDiskTables(DiskTable first, DiskTable second)
        {
            var mergedConfiguration = CreateDiskTableConfiguration();
            return await DiskTable.DumpItems(mergedConfiguration, first.Level + second.Level, first.GetAllItems().MergeWith(second.GetAllItems()));
        }
    }
}
