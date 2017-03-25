using DataLayer.MemoryCopy;
using DataLayer.OperationLog;
using DataLayer.OperationLog.Operations;

namespace DataLayer.Warmup
{
    public class OperationLogApplier : IOperationLogApplier
    {
        private readonly IOperationLogReader logReader;

        public OperationLogApplier(IOperationLogReader logReader)
        {
            this.logReader = logReader;
        }

        public void Apply(IDataWriter memoryTable)
        {
            IOperation operation;
            while (logReader.Read(out operation))
                operation.Apply(memoryTable);
        }
    }
}