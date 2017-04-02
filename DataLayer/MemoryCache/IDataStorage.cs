namespace DataLayer.MemoryCache
{
    public interface IDataStorage : IDataWriter, IDataReader
    {
        int Size { get; }
    }
}