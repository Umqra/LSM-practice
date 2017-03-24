using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.DataModel;
using DataLayer.MemoryCopy;

namespace DataLayer.OperationLog.Operations
{
    public class DeleteOperation : IOperation
    {
        public Item Item { get; }

        public DeleteOperation(Item item)
        {
            Item = item;
        }

        public void Apply(IMemoryTable memoryTable)
        {
            memoryTable.Add(Item);
        }

        private bool Equals(DeleteOperation other)
        {
            return Equals(Item, other.Item);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DeleteOperation)obj);
        }

        public override int GetHashCode()
        {
            return Item?.GetHashCode() ?? 0;
        }

        public override string ToString()
        {
            return $"Delete: {Item}";
        }
    }
}
