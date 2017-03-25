using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.DataModel;
using DataLayer.OperationLog;
using DataLayer.Utilities;

namespace DataLayer.MemoryCopy
{
    public class CacheManagerConfiguration
    {
        public DirectoryInfo WorkingDirectory { get; set; }
        public Func<FileInfo, bool> OperationLogFilter { get; set; }
    }
    public class CacheManager : IDataStorage, IDisposable
    {
        private CacheManagerConfiguration configuration;
        private Cache currentCache;
        private IFile cacheLogFile;
        public CacheManager(CacheManagerConfiguration configuration)
        {
            this.configuration = configuration;
            InitializeCache();
        }

        private void InitializeCache()
        {
            foreach (var file in configuration.WorkingDirectory.GetFiles().Where(configuration.OperationLogFilter))
            {
                var logFile = new File(file.FullName);
                using (var reader = new OperationLogReader())
            }
        }


        public void Add(Item item)
        {
            currentCache.Add(item);
        }

        public void Delete(string key)
        {
            currentCache.Delete(key);
        }

        public Item Get(string key)
        {
            return currentCache.Get(key);
        }

        public IEnumerable<Item> GetAllItems()
        {
            return currentCache.GetAllItems();
        }

        public void Dispose()
        {
            currentCache?.Dispose();
        }
    }
}
