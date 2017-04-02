using System.IO.Abstractions;
using DataLayer.Utilities;

namespace DataLayer.OperationLog
{
    public interface IOperationLogRepairer
    {
        /// <summary>
        /// Repairs log after sudden shutdown
        /// </summary>
        /// <param name="logFile">File with operation logs</param>
        void RepairLog(FileInfoBase logFile);
    }
}