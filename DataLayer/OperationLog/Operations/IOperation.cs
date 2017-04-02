using DataLayer.MemoryCache;

namespace DataLayer.OperationLog.Operations
{
    public interface IOperation
    {
        void Apply(IDataWriter memoryTable);
    }
}