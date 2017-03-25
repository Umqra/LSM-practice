using System.Collections.Generic;
using DataLayer.DataModel;

namespace DataLayer.MemoryCopy
{
    public interface IDataReader
    {
        Item Get(string key);
        IEnumerable<Item> GetAllItems();
    }

    public interface IDataWriter
    {
        void Add(Item item);
        void Delete(string key);
    }

    public interface IDataStorage : IDataWriter, IDataReader
    {
        int Size { get; }
    }
}