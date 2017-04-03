using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataLayer;
using DataLayer.DataModel;
using DataLayer.MemoryCache;
using DataLayer.OperationLog;
using DataLayer.OperationLog.Operations;

namespace LSMpractice
{
    class Program
    {
        static IEnumerable<int> Sequence(int start, int step, int count)
        {
            return Enumerable.Range(0, count).Select(i => start + i * step);
        }
        static void Main(string[] args)
        {
            var directory = Directory.CreateDirectory("database");
            var database = new Database(directory, new SizeDumpCriteria(10), new FileInfoFactory());
            for (int i = 1; i <= 100; i++)
                database.Add(Item.CreateItem(i.ToString(), i.ToString()));
            while (true)
            {
                string line = Console.ReadLine();
                var tokens = line.Split().ToList();
                if (tokens[0] == "get")
                    Console.WriteLine(database.Get(tokens[1]));
                else if (tokens[0] == "delete")
                    database.Delete(tokens[1]);
                else if (tokens[0] == "add")
                    database.Add(Item.CreateItem(tokens[1], tokens[2]));
            }
        }
    }
}
