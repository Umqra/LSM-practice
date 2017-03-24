using System;
using DataLayer;
using DataLayer.DataModel;
using DataLayer.MemoryCopy;
using DataLayer.OperationLog;
using DataLayer.OperationLog.Operations;
using DataLayer.Utilities;
using DataLayer.Warmup;
using FluentAssertions;
using NUnit.Framework;

namespace DataLayerTests
{
    [TestFixture]
    public class OpLogApplierTests
    {
        private string filePath;

        [SetUp]
        public void SetUp()
        {
            filePath = Guid.NewGuid().ToString();
        }

        [TearDown]
        public void TearDown()
        {
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }


        [Test]
        public void Should_apply_operation_from_opLog()
        {
            var item1 = Item.CreateItem(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            var item2 = Item.CreateItem(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

            using (var memoryTable = MemoryTableManager.RestoreFromOperationLog(new File(filePath)))
            {
                memoryTable.Add(item1);
                memoryTable.Add(item2);
            }

            using (var memoryTable = MemoryTableManager.RestoreFromOperationLog(new File(filePath)))
            {

                var itemFromTable1 = memoryTable.Get(item1.Key);
                var itemFromTable2 = memoryTable.Get(item2.Key);

                itemFromTable1.Should().Be(item1);
                itemFromTable2.Should().Be(item2);
            }
        }
    }
}
