using System;
using System.IO;
using DataLayer.DataModel;
using DataLayer.MemoryCopy;
using DataLayer.OperationLog;
using DataLayer.OperationLog.Operations;
using DataLayer.Warmup;
using File = DataLayer.Utilities.File;

namespace LSMpractice
{
    class Program
    {
        static void Main(string[] args)
        {
            var manager = MemoryTableManager.RestoreFromOperationLog(new File("operations.log"));
            foreach (var item in manager.GetAllItems())
                Console.WriteLine(item);
            while (true)
            {
                var key = Console.ReadLine();
                var value = Console.ReadLine();
                manager.Add(Item.CreateItem(key, value));
            }
        }
    }
}
