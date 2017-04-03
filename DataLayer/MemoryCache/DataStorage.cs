using System.Collections.Concurrent;
using System.Collections.Generic;
using DataLayer.DataModel;

namespace DataLayer.MemoryCache
{
    public class DataStorage : IDataStorage
    {
        private readonly SortedDictionary<string, Item> storage;
        public int Size => storage.Count;

        public DataStorage()
        {
            storage = new SortedDictionary<string, Item>();
        }

        public void Add(Item item)
        {
            lock(storage)
                storage[item.Key] = item;
        }

        public Item Get(string key)
        {
            Item result;
            lock (storage)
            {
                if (!storage.TryGetValue(key, out result))
                    return null;
            }
            if (result.IsTombStone)
                return null;
            return result;
        }

        public void Delete(string key)
        {
            lock (storage)
            {
                storage[key] = Item.CreateTombStone(key);
            }
        }

        public IEnumerable<Item> GetAllItems()
        {
            lock(storage)
                return storage.Values;
        }
    }
}