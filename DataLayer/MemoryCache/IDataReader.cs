using System.Collections.Generic;
using DataLayer.DataModel;

namespace DataLayer.MemoryCache
{
    public interface IDataReader
    {
        Item Get(string key);
        IEnumerable<Item> GetAllItems();
    }
}