using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C5;
using DataLayer.DataModel;
using DataLayer.MemoryCache;

namespace DataLayer.DiskTable
{
    public class DiskTableIndex
    {
        public TreeDictionary<string, long> TableIndex { get; }

        public DiskTableIndex() : this(new TreeDictionary<string, long>()) { }

        public DiskTableIndex(TreeDictionary<string, long> tableIndex)
        {
            TableIndex = tableIndex;
        }

        //TODO: make index serializer?
        public async Task Serialize(Stream stream)
        {
            var bytes = new List<byte>();
            var startPosition = stream.Position;
            bytes.AddRange(BitConverter.GetBytes(TableIndex.Count));
            foreach (var indexEntry in TableIndex.RangeAll())
            {
                var keyBytes = Encoding.UTF8.GetBytes(indexEntry.Key);
                bytes.AddRange(BitConverter.GetBytes(keyBytes.Length));
                bytes.AddRange(keyBytes);
                bytes.AddRange(BitConverter.GetBytes(indexEntry.Value));
            }
            bytes.AddRange(BitConverter.GetBytes(startPosition));
            await stream.WriteAsync(bytes.ToArray(), 0, bytes.Count);
        }

        public static async Task<DiskTableIndex> Deserialize(Stream stream)
        {
            stream.Seek(-8, SeekOrigin.End);
            var startPosition = await stream.ReadLongAsync();
            stream.Seek(startPosition, SeekOrigin.Begin);
            var count = await stream.ReadIntAsync();
            var indexEntries = new List<C5.KeyValuePair<string, long>>();
            for (int i = 0; i < count; i++)
            {
                var keyLength = await stream.ReadIntAsync();
                var key = await stream.ReadStringAsync(keyLength, Encoding.UTF8);
                var value = await stream.ReadLongAsync();
                indexEntries.Add(C5.KeyValuePair.Create(key, value));
            }
            var tableIndex = new TreeDictionary<string, long>();
            tableIndex.AddAll(indexEntries);
            return new DiskTableIndex(tableIndex);
        }
    }

    public class DiskTable : IDataReader
    {
        private readonly DiskTableConfiguration configuration;
        private readonly DiskTableIndex tableIndex;
        public int Level { get; }
        private C5.KeyValuePair<string, long> MinKey => tableIndex.TableIndex.FindMin();
        private C5.KeyValuePair<string, long> MaxKey => tableIndex.TableIndex.FindMax();

        public DiskTable(DiskTableConfiguration configuration, int level, DiskTableIndex tableIndex)
        {
            this.configuration = configuration;
            Level = level;
            this.tableIndex = tableIndex;
        }

        public Item Get(string key)
        {
            if (key.LessThan(MinKey.Key) || MaxKey.Key.LessThan(key))
                return null;
            var predecessor = tableIndex.TableIndex.WeakPredecessor(key);
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

        public static async Task<DiskTable> DumpCache(DiskTableConfiguration configuration, Cache cache)
        {
            return await DumpItems(configuration, 0, cache.GetAllItems());
        }

        public static async Task<DiskTable> DumpItems(DiskTableConfiguration configuration, int level,
            IEnumerable<Item> items)
        {
            var tableIndex = new DiskTableIndex();
            using (var stream = configuration.TableFile.Open(FileMode.OpenOrCreate, FileAccess.Write))
            {
                await stream.WriteAsync(BitConverter.GetBytes(level), 0, 4);
                foreach (var itemGroup in items.GroupBySize(configuration.IndexSpanSize))
                {
                    var singleGroup = itemGroup.ToList();
                    var positionBeforeWrite = stream.Position;
                    tableIndex.TableIndex[singleGroup.First().Key] = positionBeforeWrite;
                    foreach (var item in singleGroup)
                    {
                        positionBeforeWrite = stream.Position;
                        var serialized = configuration.Serializer.Serialize(item);
                        await stream.WriteAsync(serialized, 0, serialized.Length);
                    }
                    tableIndex.TableIndex[singleGroup.Last().Key] = positionBeforeWrite;
                }
                await tableIndex.Serialize(stream);
            }
            return new DiskTable(configuration, level, tableIndex);
        }

        public void Delete()
        {
            configuration.TableFile.Delete();
        }
    }
}