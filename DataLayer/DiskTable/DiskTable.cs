using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using C5;
using DataLayer.DataModel;
using DataLayer.MemoryCopy;

namespace DataLayer.DiskTable
{
    public class DiskTable
    {
        private readonly DiskTableConfiguration configuration;
        private readonly TreeDictionary<string, long> tableIndex;
        private C5.KeyValuePair<string, long> MinKey => tableIndex.FindMin();
        private C5.KeyValuePair<string, long> MaxKey => tableIndex.FindMax();

        public DiskTable(DiskTableConfiguration configuration, TreeDictionary<string, long> tableIndex)
        {
            this.configuration = configuration;
            this.tableIndex = tableIndex;
        }

        public static async Task<DiskTable> DumpCache(DiskTableConfiguration configuration, Cache cache)
        {
            var tableIndex = new TreeDictionary<string, long>();
            using (var stream = configuration.TableFile.Open(FileMode.OpenOrCreate, FileAccess.Write))
            {
                foreach (var itemGroup in cache.GetAllItems().GroupBySize(configuration.IndexSpanSize))
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
            return new DiskTable(configuration, tableIndex);
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
    }
}