using System.Collections.Concurrent;
using System.Collections.Generic;
using DataLayer.DataModel;

namespace DataLayer.MemoryCache
{
    public class DataStorage : IDataStorage
    {
        private readonly ConcurrentDictionary<string, Item> storage;
        public int Size => storage.Count;

        public DataStorage()
        {
            storage = new ConcurrentDictionary<string, Item>();
        }

        public void Add(Item item)
        {
            storage[item.Key] = item;
        }

        public Item Get(string key)
        {
            Item result;
            if (!storage.TryGetValue(key, out result))
                return null;
            if (result.IsTombStone)
                return null;
            return result;
        }

        public void Delete(string key)
        {
            storage[key] = Item.CreateTombStone(key);
        }

        public IEnumerable<Item> GetAllItems()
        {
            return storage.Values;
        }
    }
}