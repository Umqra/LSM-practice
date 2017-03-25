using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DataLayer.DataModel;
using DataLayer.OperationLog;
using DataLayer.OperationLog.Operations;
using DataLayer.Utilities;
using DataLayer.Warmup;

namespace DataLayer.MemoryCopy
{
    public class Cache : IDataStorage, IDisposable
    {
        private readonly IOperationLogWriter logWriter;
        private readonly IDataStorage memoryTable;

        private Cache(IOperationLogWriter logWriter, IDataStorage memoryTable)
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
    }
}