using System;
using System.IO;
using DataLayer.DataModel;
using DataLayer.MemoryCache;
using DataLayer.OperationLog;
using DataLayer.OperationLog.Operations;
using DataLayer.Utilities;
using FakeItEasy;
using FluentAssertions;
using NUnit.Framework;

namespace DataLayerTests
{
    [TestFixture]
    public class MemoryCacheTests
    {
        private Cache cache;
        private IOperationLogWriter logWriter;
        
        [SetUp]
        public void SetUp()
        {
            logWriter = A.Fake<IOperationLogWriter>();
            cache = new Cache(logWriter, new DataStorage());
        }

        [TearDown]
        public void TearDown()
        {
            cache.Dispose();
        }

        [Test]
        public void TestAddingItems()
        {
            var item1 = Item.CreateItem(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            var item2 = Item.CreateItem(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

            cache.Add(item1);
            cache.Add(item2);

            var itemFromTable1 = cache.Get(item1.Key);
            var itemFromTable2 = cache.Get(item2.Key);

            itemFromTable1.Should().Be(item1);
            itemFromTable2.Should().Be(item2);
        }

        [Test]
        public void TestAddingSameKeys()
        {
            var key = Guid.NewGuid().ToString();
            var item1 = Item.CreateItem(key, Guid.NewGuid().ToString());
            var item2 = Item.CreateItem(key, Guid.NewGuid().ToString());

            cache.Add(item1);
            cache.Add(item2);
            cache.Get(key).Should().Be(item2);
        }

        [Test]
        public void TestDeletingItems()
        {
            var key = Guid.NewGuid().ToString();
            var item = Item.CreateItem(key, Guid.NewGuid().ToString());

            cache.Add(item);
            cache.Delete(key);
            cache.Get(key).Should().Be(null);
        }


        [Test]
        public void TestDumpPreparation()
        {
            cache.PrepareToDump();
            A.CallTo(() => logWriter.Write(A<IOperation>.That.Matches(op => op is DumpOperation))).MustHaveHappened();
        }
    }
}
