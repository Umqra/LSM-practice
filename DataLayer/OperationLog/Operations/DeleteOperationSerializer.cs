using System;
using System.IO;
using System.Text;
using DataLayer.DataModel;

namespace DataLayer.OperationLog.Operations
{
    public static class StreamExtensions
    {
        public static byte[] ReadExactly(this BinaryReader stream, int count)
        {
            byte[] bytes = stream.ReadBytes(count);
            if (bytes.Length < count)
                throw new EndOfStreamException();
            return bytes;
        }
    }
    public class DeleteOperationSerializer : IOperationSerializer
    {
        public byte[] Serialize(IOperation operation)
        {
            var deleteOperation = (DeleteOperation)operation;
            var keyBytes = Encoding.UTF8.GetBytes(deleteOperation.Item.Key);
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(BitConverter.GetBytes(keyBytes.Length));
                writer.Write(keyBytes);
                writer.Flush();
                return stream.ToArray();
            }
        }

        public IOperation Deserialize(Stream logStream)
        {
            var reader = new BinaryReader(logStream);
            int keyLength = reader.ReadInt32();
            var key = Encoding.UTF8.GetString(reader.ReadExactly(keyLength));
            return new DeleteOperation(Item.CreateTombStone(key));
        }
    }
}