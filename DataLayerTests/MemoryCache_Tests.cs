using System;
using DataLayer.DataModel;
using DataLayer.MemoryCache;
using DataLayer.Utilities;
using FluentAssertions;
using NUnit.Framework;

namespace DataLayerTests
{
    [TestFixture]
    public class MemoryCacheTests
    {
        private Cache table;
        private string filePath;

        [SetUp]
        public void SetUp()
        {
            filePath = Guid.NewGuid().ToString();
            //table = Cache.RestoreFromOperationLog(new FileData(filePath));
        }

        [TearDown]
        public void TearDown()
        {
            table.Dispose();
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        [Test]
        public void Should_add_items()
        {
            var item1 = Item.CreateItem(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            var item2 = Item.CreateItem(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

            table.Add(item1);
            table.Add(item2);

            var itemFromTable1 = table.Get(item1.Key);
            var itemFromTable2 = table.Get(item2.Key);

            itemFromTable1.Should().Be(item1);
            itemFromTable2.Should().Be(item2);
        }

        [Test]
        public void Should_overwrite_item_with_same_key()
        {
            var key = Guid.NewGuid().ToString();
            var item = Item.CreateItem(key, Guid.NewGuid().ToString());

            table.Add(item);
            table.Get(key).Should().Be(item);

            table.Delete(key);
            table.Get(key).Should().Be(null);
        }
    }
}
