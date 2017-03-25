using System;
using System.Collections.Generic;
using DataLayer.DataModel;
using DataLayer.OperationLog;
using DataLayer.OperationLog.Operations;

namespace DataLayer.MemoryCopy
{
    public class Cache : IDataStorage, IDisposable
    {
        public int Size => memoryTable.Size;
        private readonly IOperationLogWriter logWriter;
        private readonly IDataStorage memoryTable;

        public Cache(IOperationLogWriter logWriter, IDataStorage memoryTable)
        {
            this.logWriter = logWriter;
            this.memoryTable = memoryTable;
        }
        
        public void Add(Item item)
        {
            logWriter.Write(new AddOperation(item));
            memoryTable.Add(item);
        }

        public void Delete(string key)
        {
            var tombStone = Item.CreateTombStone(key);
            logWriter.Write(new DeleteOperation(tombStone));
            memoryTable.Delete(key);
        }

        public Item Get(string key)
        {
            return memoryTable.Get(key);
        }

        public IEnumerable<Item> GetAllItems()
        {
            return memoryTable.GetAllItems();
        }

        public void Dispose()
        {
            logWriter?.Dispose();
        }

        public void PrepareToDump()
        {
            logWriter.Write(new DumpOperation());
        }
    }
}