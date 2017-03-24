using System;
using DataLayer.DataModel;
using DataLayer.MemoryCopy;
using DataLayer.Utilities;
using FluentAssertions;
using NUnit.Framework;

namespace DataLayerTests
{
    [TestFixture]
    public class MemTableTests
    {
        private MemoryTableManager memoryTable;
        private string filePath;

        [SetUp]
        public void SetUp()
        {
            filePath = Guid.NewGuid().ToString();
            memoryTable = MemoryTableManager.RestoreFromOperationLog(new File(filePath));
        }

        [TearDown]
        public void TearDown()
        {
            memoryTable.Dispose();
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        [Test]
        public void Should_add_items()
        {
            var item1 = Item.CreateItem(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            var item2 = Item.CreateItem(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

            memoryTable.Add(item1);
            memoryTable.Add(item2);

            var itemFromTable1 = memoryTable.Get(item1.Key);
            var itemFromTable2 = memoryTable.Get(item2.Key);

            itemFromTable1.Should().Be(item1);
            itemFromTable2.Should().Be(item2);
        }

        [Test]
        public void Should_overwrite_item_with_same_key()
        {
            var key = Guid.NewGuid().ToString();
            var item = Item.CreateItem(key, Guid.NewGuid().ToString());

            memoryTable.Add(item);
            memoryTable.Get(key).Should().Be(item);

            memoryTable.Delete(key);
            memoryTable.Get(key).Should().Be(null);
        }
    }
}
