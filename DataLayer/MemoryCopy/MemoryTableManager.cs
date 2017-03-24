using System;
using System.Collections.Generic;
using System.IO;
using DataLayer.DataModel;
using DataLayer.OperationLog;
using DataLayer.OperationLog.Operations;
using DataLayer.Utilities;
using DataLayer.Warmup;

namespace DataLayer.MemoryCopy
{
    public class MemoryTableManager : IMemoryTable, IDisposable
    {
        private readonly IOperationLogWriter logWriter;
        private readonly IMemoryTable memoryTable;

        public MemoryTableManager(IOperationLogWriter logWriter, IMemoryTable memoryTable)
        {
            this.logWriter = logWriter;
            this.memoryTable = memoryTable;
        }

        public static MemoryTableManager RestoreFromOperationLog(IFile logFile)
        {
            new OperationLogRepairer().RepairLog(logFile);
            var memoryTable = new MemoryTable();
            using (var reader = new OperationLogReader(
                logFile.GetStream(FileMode.OpenOrCreate, FileAccess.Read), new OperationSerializer()))
            {
                new OperationLogApplier(reader).Apply(memoryTable);
            }
            return new MemoryTableManager(
                new OperationLogWriter(logFile.GetStream(FileMode.Append, FileAccess.Write), new OperationSerializer()),
                memoryTable);
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