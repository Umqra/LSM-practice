using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.DataModel;
using DataLayer.MemoryCache;

namespace DataLayer.OperationLog.Operations
{
    public class AddOperation : IOperation
    {
        public Item Item { get; }

        public AddOperation(Item item)
        {
            Item = item;
        }

        public void Apply(IDataWriter memoryTable)
        {
            memoryTable.Add(Item);
        }

        private bool Equals(AddOperation other)
        {
            return Equals(Item, other.Item);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AddOperation)obj);
        }

        public override int GetHashCode()
        {
            return Item?.GetHashCode() ?? 0;
        }

        public override string ToString()
        {
            return $"Add: {Item}";
        }
    }
}
