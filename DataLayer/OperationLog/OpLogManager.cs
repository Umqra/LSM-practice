using System;
using System.IO;
using DataLayer.OperationLog.Operations;
using DataLayer.Utilities;

namespace DataLayer.OperationLog
{
    public class OpLogManager : IOpLogReader, IOpLogWriter, IDisposable
    {
        private readonly IFile logFile;
        private readonly IOperationSerializer serializer;
        private readonly Lazy<Stream> readStream;
        private readonly Lazy<Stream> writeStream;
        private bool disposed;

        public OpLogManager(IFile logFile, IOperationSerializer serializer)
        {
            this.logFile = logFile;
            this.serializer = serializer;
            readStream = new Lazy<Stream>(() => logFile.GetStream(FileAccess.Read));
            writeStream = new Lazy<Stream>(() => logFile.GetStream(FileAccess.Write));
        }

        public bool Read(out IOperation operation)
        {
            operation = serializer.Deserialize(readStream.Value);
            return operation != null;
        }

        public void Write(IOperation operation)
        {
            var serialized = serializer.Serialize(operation);
            writeStream.Value.Write(serialized, 0, serialized.Length);
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            if (readStream.IsValueCreated)
                readStream.Value.Dispose();
            if (writeStream.IsValueCreated)
                writeStream.Value.Dispose();
        }
    }
}