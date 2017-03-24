using System;
using System.IO;
using DataLayer.OperationLog.Operations;

namespace DataLayer.OperationLog
{
    public class OperationLogWriter : IOperationLogWriter
    {
        private readonly IOperationSerializer serializer;
        private readonly Stream logStream;

        public OperationLogWriter(Stream logStream, IOperationSerializer serializer)
        {
            this.serializer = serializer;
            this.logStream = logStream;
        }

        public void Write(IOperation operation)
        {
            var serialized = serializer.Serialize(operation);
            logStream.Write(serialized, 0, serialized.Length);
        }

        public void Dispose()
        {
            logStream?.Dispose();
        }
    }
}