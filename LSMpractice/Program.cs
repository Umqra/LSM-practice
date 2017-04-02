using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataLayer;
using DataLayer.DataModel;
using DataLayer.OperationLog;
using DataLayer.OperationLog.Operations;

namespace LSMpractice
{
    class Program
    {
        static void Main(string[] args)
        {
            Directory.CreateDirectory("database");
            var database = new Database("./database");
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
