using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.OperationLog.Operations
{
    public class DumpOperationSerializer : IOperationSerializer
    {
        public byte[] Serialize(IOperation operation)
        {
            return new byte[0];
        }

        public IOperation Deserialize(Stream logStream)
        {
            return new DumpOperation();
        }
    }
}
