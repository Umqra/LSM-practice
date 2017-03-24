using System.Collections.Concurrent;
using System.Collections.Generic;
using DataLayer.DataModel;

namespace DataLayer.MemoryCopy
{
    public class MemoryTable : IMemoryTable
    {
        private readonly ConcurrentDictionary<string, Item> memTable;

        public int Size => memTable.Count;

        public MemoryTable()
        {
            memTable = new ConcurrentDictionary<string, Item>();
        }

        public void Add(Item item)
        {
            memTable[item.Key] = item;
        }

        public Item Get(string key)
        {
            Item result;
            if (!memTable.TryGetValue(key, out result))
                return null;
            if (result.IsTombStone)
                return null;
            return result;
        }

        public void Delete(string key)
        {
            memTable[key] = Item.CreateTombStone(key);
        }

        public IEnumerable<Item> GetAllItems()
        {
            return memTable.Values;
        }
    }
}