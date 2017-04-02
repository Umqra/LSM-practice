using System;
using System.IO;
using System.IO.Abstractions;
using DataLayer.OperationLog.Operations;
using DataLayer.Utilities;

namespace DataLayer.OperationLog
{
    public class OperationLogRepairer : IOperationLogRepairer
    {
        public void RepairLog(FileInfoBase logFile)
        {
            long validLength = 0;
            using (var fileReader = logFile.Open(FileMode.OpenOrCreate, FileAccess.Read))
            {
                IOperation operation;
                var reader = new OperationLogReader(fileReader, new OperationSerializer());
                while (reader.Read(out operation))
                    validLength = fileReader.Position;
            }
            using (var fileWriter = logFile.Open(FileMode.OpenOrCreate, FileAccess.Write))
            {
                fileWriter.SetLength(validLength);
            }
        }
    }
}
