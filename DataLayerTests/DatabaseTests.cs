using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataLayer;
using DataLayer.DataModel;
using DataLayer.MemoryCache;
using DataLayer.OperationLog.Operations;
using FluentAssertions;
using NUnit.Framework.Internal;
using NUnit.Framework;

namespace DataLayerTests
{
    [TestFixture]
    class DatabaseTests
    {
        private MockDirectoryInfo directory;
        private MockFileSystem fileSystem;

        [SetUp]
        public void SetUp()
        {
            fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>());
            fileSystem.AddDirectory(@"c:\path");
            directory = new MockDirectoryInfo(fileSystem, @"c:\path");
        }

        private Database CreateDatabase(int dumpSize = 10)
        {
            return new Database(directory, new SizeDumpCriteria(dumpSize), new MockFileInfoFactory(fileSystem));
        }

        [Test]
        public void TestOperationLogCreation()
        {
            using (var database = CreateDatabase())
            {
                database.Add(Item.CreateItem("a", "b"));
                directory.GetFiles().Select(f => f.Name).ToArray().Should().BeEquivalentTo("log-1.txt");
            }
        }

        [Test]
        public void TestRecoveryFromOperationLog()
        {
            var items = new[]
            {
                Item.CreateItem("a", "b"),
                Item.CreateItem("d", "123")
            };
            using (var database = CreateDatabase())
            {
                database.Add(items[0]);
                database.Add(items[1]);
            }
            using (var database = CreateDatabase())
            {
                database.Get(items[0].Key).Should().Be(items[0]);
                database.Get(items[1].Key).Should().Be(items[1]);
            }
        }

        [Test]
        public void TestDiskTableCreation()
        {
            var items = new[]
            {
                Item.CreateItem("a", "b"),
                Item.CreateItem("c", "d"),
                Item.CreateItem("e", "f")
            };
            using (var database = CreateDatabase(2))
            {
                foreach (var item in items)
                    database.Add(item);
            }
            Task.Delay(200).Wait();
            directory.GetFiles().Select(f => f.Name).Should().BeEquivalentTo(
                "log-2.txt",
                "sstable-1.txt");
        }

        [Test]
        public void TestRecoveryFromDiskTable()
        {
            var items = new[]
            {
                Item.CreateItem("a", "b"),
                Item.CreateItem("c", "d"),
                Item.CreateItem("e", "f")
            };
            using (var database = CreateDatabase(2))
            {
                foreach (var item in items)
                    database.Add(item);
            }
            Task.Delay(200).Wait();

            using (var database = CreateDatabase())
            {
                foreach (var item in items)
                    database.Get(item.Key).Should().Be(item);
            }
        }

        [Test]
        public void TestManyDiskTablesCreation()
        {
            var items = new[]
            {
                Item.CreateItem("a", "b"),
                Item.CreateItem("c", "d"),
                Item.CreateItem("e", "f"),
                Item.CreateItem("g", "h")
            };
            using (var database = CreateDatabase(1))
            {
                foreach (var item in items)
                    database.Add(item);
            }
            Task.Delay(200).Wait();

            directory.GetFiles().Where(f => f.Exists).Select(f => f.Name).Should().BeEquivalentTo(
                "log-3.txt",
                "sstable-3.txt");
        }

        [Test]
        public void TestRandomOperations()
        {
            int count = 1000;
            var dictionary = new Dictionary<string, string>();
            var random = new Random(0);
            using (var database = CreateDatabase(20))
            {
                for (int i = 0; i < count; i++)
                {
                    var key = random.Next(100).ToString();
                    var type = random.Next(2);
                    if (type == 0)
                    {
                        database.Delete(key);
                        dictionary.Remove(key);
                    }
                    else if (type == 1)
                    {
                        var value = random.Next(10).ToString();
                        database.Add(Item.CreateItem(key, value));
                        dictionary[key] = value;
                    }
                    else
                    {
                        database.Get(key).Value.Should().Be(dictionary[key]);
                    }
                }
            }
        }
    }
}
