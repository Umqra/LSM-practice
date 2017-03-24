using DataLayer.OperationLog.Operations;

namespace DataLayer.OperationLog
{
    public interface IOpLogWriter
    {
        void Write(IOperation operation);
    }
}