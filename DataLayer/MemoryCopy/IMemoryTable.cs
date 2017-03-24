using System.Collections.Generic;
using DataLayer.DataModel;

namespace DataLayer.MemoryCopy
{
    public interface IMemoryTable
    {
        void Add(Item item);

        Item Get(string key);

        void Delete(string key);

        IEnumerable<Item> GetAllItems();
    }
}