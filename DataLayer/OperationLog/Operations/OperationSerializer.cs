using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataLayer.OperationLog.Operations
{
    public class OperationSerializer : IOperationSerializer
    {
        private readonly Dictionary<Type, IOperationSerializer> serializerByOperationType =
            new Dictionary<Type, IOperationSerializer>
            {
                [typeof(AddOperation)] = new AddOperationSerializer(),
                [typeof(DeleteOperation)] = new DeleteOperationSerializer(),
                [typeof(DumpOperation)] = new DumpOperationSerializer(),
            };
        private readonly Dictionary<Type, int> operationHeader = new Dictionary<Type, int>
        {
            [typeof(AddOperation)] = 1,
            [typeof(DeleteOperation)] = 2,
            [typeof(DumpOperation)] = 3,
        };

        public byte[] Serialize(IOperation operation)
        {
            var type = operation.GetType();
            if (!serializerByOperationType.ContainsKey(type))
                throw new ArgumentException($"Unknown IOperation for serialization: {type}");
            return GetOperationHeader(type)
                .Concat(serializerByOperationType[type].Serialize(operation))
                .ToArray();
        }

        private byte[] GetOperationHeader(Type type)
        {
            return new[] {(byte)operationHeader[type]};
        }

        private IOperationSerializer GetSerializer(Stream logStream)
        {
            var header = logStream.ReadByte();
            if (header == -1)
                return null;
            return GetSerializerByOperationHeader((byte)header);
        }

        private IOperationSerializer GetSerializerByOperationHeader(byte header)
        {
            foreach (var item in operationHeader)
            {
                if (item.Value == header)
                    return serializerByOperationType[item.Key];
            }
            throw new ArgumentException($"Can't find suitable {nameof(IOperationSerializer)} for header {header}");
        }

        public IOperation Deserialize(Stream logStream)
        {
            var serializer = GetSerializer(logStream);
            return serializer?.Deserialize(logStream);
        }
    }
}