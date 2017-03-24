using System.IO;
using DataLayer.OperationLog.Operations;

namespace DataLayer.OperationLog
{
    public class OperationLogReader : IOperationLogReader
    {
        private readonly IOperationSerializer serializer;
        private readonly Stream logStream;

        public OperationLogReader(Stream logStream, IOperationSerializer serializer)
        {
            this.serializer = serializer;
            this.logStream = logStream;
        }

        public bool Read(out IOperation operation)
        {
            try
            {
                operation = serializer.Deserialize(logStream);
                return operation != null;
            }
            catch (EndOfStreamException)
            {
                operation = null;
                return false;
            }
        }

        public void Dispose()
        {
            logStream?.Dispose();
        }
    }
}