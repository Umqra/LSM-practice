using System;
using DataLayer.OperationLog.Operations;

namespace DataLayer.OperationLog
{
    public interface IOperationLogWriter : IDisposable
    {
        /// <summary>
        /// Writes single operation to log
        /// </summary>
        /// <param name="operation"></param>
        void Write(IOperation operation);
    }
}