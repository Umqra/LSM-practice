namespace DataLayer.MemoryCache
{
    public interface IDumpCriteria
    {
        bool ShouldDump(Cache cache);
    }
}