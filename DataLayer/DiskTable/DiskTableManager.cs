using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.DataModel;
using DataLayer.MemoryCopy;
using DataLayer.Utilities;

namespace DataLayer.DiskTable
{
    public class DiskTableManagerConfiguraiton
    {
        public DirectoryInfo WorkingDirectory { get; set; }
        public Func<string> UniqueNameGenerator { get; set; }
    }
    public class DiskTableManager : IDiskTableManager
    {
        private readonly DiskTableManagerConfiguraiton configuration;
        private const int DefaultIndexSpanSize = 100;
        private readonly List<DiskTable> diskTables;
        private readonly List<Cache> pendingCaches;

        public DiskTableManager(DiskTableManagerConfiguraiton configuration)
        {
            this.configuration = configuration;
            diskTables = new List<DiskTable>();
            pendingCaches = new List<Cache>();
        }
        public Item Get(string key)
        {
            foreach (var cache in pendingCaches)
            {
                var cacheValue = cache.Get(key);
                if (cacheValue != null)
                    return cacheValue;
            }
            foreach (var diskTable in diskTables)
            {
                var tableValue = diskTable.Get(key);
                if (tableValue != null)
                    return tableValue;
            }
            return null;
        }

        public IEnumerable<Item> GetAllItems()
        {
            throw new NotImplementedException();
        }

        public void DumpCache(Cache cache, Action cleanupAction)
        {
            var diskTableConfig = CreateDiskTableConfiguration();
            pendingCaches.Add(cache);
            Task.Run(async () => await DiskTable.DumpCache(diskTableConfig, cache)).ContinueWith(t =>
            {
                diskTables.Add(t.Result);
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
                TableFile = new FileData(path)
            };
        }
    }
}
