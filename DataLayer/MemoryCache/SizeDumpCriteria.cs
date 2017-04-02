namespace DataLayer.MemoryCache
{
    public class SizeDumpCriteria : IDumpCriteria
    {
        private int MaxCacheSize { get; }

        public SizeDumpCriteria(int maxCacheSize)
        {
            MaxCacheSize = maxCacheSize;
        }

        public bool ShouldDump(Cache cache)
        {
            return cache.Size > MaxCacheSize;
        }
    }
}