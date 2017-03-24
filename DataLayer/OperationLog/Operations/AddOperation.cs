using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.DataModel;
using DataLayer.MemoryCopy;

namespace DataLayer.OperationLog.Operations
{
    public class AddOperation : IOperation
    {
        public Item Item { get; set; }

        public AddOperation(Item item)
        {
            Item = item;
        }

        public void Apply(IMemTable memTable)
        {
            throw new NotImplementedException();
        }
    }
}
