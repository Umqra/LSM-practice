using System.Collections.Generic;
using System.IO;
using System.Linq;
using C5;
using DataLayer.DataModel;
using DataLayer.MemoryCopy;

namespace DataLayer.SortedDiskTable
{
    public class SortedDiskTable
    {
        private readonly SortedDiskTableConfiguration configuration;
        private readonly TreeDictionary<string, long> tableIndex;
        private C5.KeyValuePair<string, long> MinKey => tableIndex.FindMin();
        private C5.KeyValuePair<string, long> MaxKey => tableIndex.FindMax();

        public SortedDiskTable(SortedDiskTableConfiguration configuration, IDataStorage memoryTable)
        {
            this.configuration = configuration;
            tableIndex = new TreeDictionary<string, long>();
            AddItems(memoryTable.GetAllItems());
        }

        private void AddItems(IEnumerable<Item> items)
        {
            using (var stream = configuration.TableFile.Open(FileMode.OpenOrCreate, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
            {
                foreach (var itemGroup in items.GroupBySize(configuration.IndexSpanSize))
                {
                    var singleGroup = itemGroup.ToList();
                    var positionBeforeWrite = stream.Position;
                    tableIndex[singleGroup.First().Key] = positionBeforeWrite;
                    foreach (var item in singleGroup)
                    {
                        positionBeforeWrite = stream.Position;
                        writer.Write(configuration.Serializer.Serialize(item));
                    }
                    tableIndex[singleGroup.Last().Key] = positionBeforeWrite;
                }
            }
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