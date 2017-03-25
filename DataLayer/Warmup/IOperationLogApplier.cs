using DataLayer.MemoryCopy;

namespace DataLayer.Warmup
{
    public interface IOperationLogApplier
    {
        void Apply(IDataWriter memoryTable);
    }
}