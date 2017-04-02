using DataLayer.DataModel;

namespace DataLayer.MemoryCache
{
    public interface IDataWriter
    {
        void Add(Item item);
        void Delete(string key);
    }
}