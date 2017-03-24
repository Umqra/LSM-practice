using System.IO;

namespace DataLayer.OperationLog.Operations
{
    //учесть что надо прыгать по определённому офсету в бинарно сериализованном файле
    // ветки с решенными этапами локально держат

    public class OperationSerializer : IOperationSerializer
    {
        public byte[] Serialize(IOperation operation)
        {
            throw new System.NotImplementedException();
        }

        public IOperation Deserialize(Stream opLogStream)
        {
            throw new System.NotImplementedException();
        }
    }
}