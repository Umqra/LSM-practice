using System;
using System.IO;
using System.Text;
using DataLayer.DataModel;

namespace DataLayer.OperationLog.Operations
{
    public class AddOperationSerializer : IOperationSerializer
    {
        public byte[] Serialize(IOperation operation)
        {
            var addOperation = (AddOperation)operation;
            var keyBytes = Encoding.UTF8.GetBytes(addOperation.Item.Key);
            var valueBytes = Encoding.UTF8.GetBytes(addOperation.Item.Value);
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(BitConverter.GetBytes(keyBytes.Length));
                writer.Write(BitConverter.GetBytes(valueBytes.Length));
                writer.Write(keyBytes);
                writer.Write(valueBytes);
                writer.Flush();
                return stream.ToArray();
            }   
        }

        public IOperation Deserialize(Stream logStream)
        {
            var reader = new BinaryReader(logStream);
            int keyLength = reader.ReadInt32();
            int valueLength = reader.ReadInt32();
            var key = Encoding.UTF8.GetString(reader.ReadExactly(keyLength));
            var value = Encoding.UTF8.GetString(reader.ReadExactly(valueLength));
            return new AddOperation(Item.CreateItem(key, value));
        }
    }
}