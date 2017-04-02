using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using C5;
using DataLayer.DataModel;
using DataLayer.MemoryCache;

namespace DataLayer.DiskTable
{
    public class DiskTable : IDataReader
    {
        private readonly DiskTableConfiguration configuration;
        public int Level { get; }
        private readonly TreeDictionary<string, long> tableIndex;
        private C5.KeyValuePair<string, long> MinKey => tableIndex.FindMin();
        private C5.KeyValuePair<string, long> MaxKey => tableIndex.FindMax();

        public DiskTable(DiskTableConfiguration configuration, int level, TreeDictionary<string, long> tableIndex)
        {
            this.configuration = configuration;
            Level = level;
            this.tableIndex = tableIndex;
        }

        public static async Task<DiskTable> DumpCache(DiskTableConfiguration configuration, Cache cache)
        {
            return await DumpItems(configuration, 0, cache.GetAllItems());
        }

        public static async Task<DiskTable> DumpItems(DiskTableConfiguration configuration, int level, IEnumerable<Item> items)
        {
            var tableIndex = new TreeDictionary<string, long>();
            using (var stream = configuration.TableFile.Open(FileMode.OpenOrCreate, FileAccess.Write))
            {
                await stream.WriteAsync(BitConverter.GetBytes(level), 0, 4);
                foreach (var itemGroup in items.GroupBySize(configuration.IndexSpanSize))
                {
                    var singleGroup = itemGroup.ToList();
                    var positionBeforeWrite = stream.Position;
                    tableIndex[singleGroup.First().Key] = positionBeforeWrite;
                    foreach (var item in singleGroup)
                    {
                        positionBeforeWrite = stream.Position;
                        var serialized = configuration.Serializer.Serialize(item);
                        await stream.WriteAsync(serialized, 0, serialized.Length);
                    }
                    tableIndex[singleGroup.Last().Key] = positionBeforeWrite;
                }
            }
            return new DiskTable(configuration, level, tableIndex);
        }

        public Item Get(string key)
        {
            if (key.LessThan(MinKey.Key) || MaxKey.Key.LessThan(key))
                return null;
            var predecessor = tableIndex.WeakPredecessor(key);
            var startOffset = predecessor.Value;
            using (var stream = configuration.TableFile.Open(FileMode.OpenOrCreate, FileAccess.Read))
            {
                stream.Seek(startOffset, SeekOrigin.Begin);
                while (stream.CanRead)
                {
                    var item = configuration.Serializer.Deserialize(stream);
                    if (item.Key == key)
                        return item;
                    if (key.LessThan(item.Key))
                        return null;
                }
            }
            return null;
        }

        public IEnumerable<Item> GetAllItems()
        {
            using (var stream = configuration.TableFile.Open(FileMode.OpenOrCreate, FileAccess.Read))
            {
                while (stream.CanRead)
                {
                    var item = configuration.Serializer.Deserialize(stream);
                    yield return item;
                }
            }
        }

        public void Delete()
        {
            configuration.TableFile.Delete();
        }
    }
}