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
using NLog;

namespace DataLayer.DiskTable
{
    public interface IDiskTableManagerConfiguration
    {
        IFileTracker DiskTablesTracker { get; }
    }
    public class DiskTableManagerConfiguration : IDiskTableManagerConfiguration
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
        private ILogger logger = LogManager.GetCurrentClassLogger();

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
            
            logger.Info("Merge disk tables: " +
                        $"{parents[0].Value.Configuration.TableFile.Name} and " +
                        $"{parents[1].Value.Configuration.TableFile.Name}");
            var merged = await diskTableManager.MergeDiskTables(parents[0].Value, parents[1].Value);
            logger.Info($"Created merge result: {merged.Configuration.TableFile.Name} after merging:" +
                        $"{parents[0].Value.Configuration.TableFile.Name} and" +
                        $"{parents[1].Value.Configuration.TableFile.Name}");

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
        private ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly IDiskTableManagerConfiguration configuration;
        private const int DefaultIndexSpanSize = 100;
        private readonly SynchronizedCollection<DiskTablesQueue> diskTableLevels;
        private readonly SynchronizedCollection<Cache> dumpingCachesQueue;

        public DiskTableManager(IDiskTableManagerConfiguration configuration)
        {
            logger.Info($"Initialize Disk Table Manager from directory: {configuration.DiskTablesTracker.WorkingDirectory.FullName}");
            this.configuration = configuration;
            diskTableLevels = new SynchronizedCollection<DiskTablesQueue>();
            dumpingCachesQueue = new SynchronizedCollection<Cache>();
            InitializeDiskTables();
        }

        private void InitializeDiskTables()
        {
            logger.Info("Load disk tables");
            foreach (var diskTableFile in configuration.DiskTablesTracker.Files)
            {
                int diskTableLevel;
                DiskTableIndex diskTableIndex;
                using (var stream = diskTableFile.OpenRead())
                {
                    var binaryReader = new BinaryReader(stream);
                    diskTableLevel = binaryReader.ReadInt32();
                    diskTableIndex = DiskTableIndex.Deserialize(stream).Result;
                }
                logger.Info($"Found disk table: {diskTableFile.Name} from level {diskTableLevel}");
                AddDiskTable(new DiskTable(new DiskTableConfiguration
                {
                    Serializer = new ItemSerializer(),
                    IndexSpanSize = DefaultIndexSpanSize,
                    TableFile = diskTableFile
                }, diskTableLevel, diskTableIndex), diskTableLevel);
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
            logger.Info($"Add disk table {diskTable.Configuration.TableFile.Name} to level {level}");
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
                    {
                        diskTableLevels[level].Remove(parent);
                        parent.Delete();
                    }
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
            return await DiskTable.DumpItems(mergedConfiguration, Math.Max(first.Level, second.Level) + 1, first.GetAllItems().MergeWith(second.GetAllItems()));
        }

        public void Dispose()
        {
            foreach (var cache in dumpingCachesQueue)
                cache.Dispose();
            foreach (var level in diskTableLevels)
            foreach (var diskTable in level)
            {
                //TODO: do something with diskTables
            }
        }
    }
}
