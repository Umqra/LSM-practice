using System.IO;
using DataLayer.OperationLog.Operations;
using DataLayer.Utilities;

namespace DataLayer.OperationLog
{
    public class OperationLogRepairer : IOperationLogRepairer
    {
        public void RepairLog(IFileData logFile)
        {
            long validLength = 0;
            using (var fileReader = logFile.GetStream(FileMode.OpenOrCreate, FileAccess.Read))
            {
                IOperation operation;
                var reader = new OperationLogReader(fileReader, new OperationSerializer());
                while (reader.Read(out operation))
                    validLength = fileReader.Position;
            }
            using (var fileWriter = logFile.GetStream(FileMode.OpenOrCreate, FileAccess.Write))
            {
                fileWriter.SetLength(validLength);
            }
        }
    }
}
