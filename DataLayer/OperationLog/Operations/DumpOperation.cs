using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.MemoryCache;

namespace DataLayer.OperationLog.Operations
{
    public class DumpOperation : IOperation
    {
        public void Apply(IDataWriter memoryTable)
        {
        }
    }
}
