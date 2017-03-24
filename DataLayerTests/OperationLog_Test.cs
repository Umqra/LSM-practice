using System.IO;
using System.Linq;
using DataLayer.DataModel;
using DataLayer.OperationLog;
using DataLayer.OperationLog.Operations;
using FluentAssertions;
using NUnit.Framework;

namespace DataLayerTests
{
    [TestFixture]
    class OperationLogTests
    {
        private MockFile file;
        private IOperationLogWriter writer;
        private IOperationLogRepairer repairer;
        [SetUp]
        public void Setup()
        {
            file = new MockFile();
            writer = GetWriter();
            repairer = new OperationLogRepairer();
        }

        private IOperationLogReader GetReader()
        {
            return new OperationLogReader(file.GetStream(FileMode.OpenOrCreate, FileAccess.Read), new OperationSerializer());
        }

        private IOperationLogWriter GetWriter()
        {
            return new OperationLogWriter(file.GetStream(FileMode.Append, FileAccess.Write), new OperationSerializer());
        }

        private static TestCaseData[] singleOperationTests = {
            new TestCaseData(new AddOperation(Item.CreateItem("a", "b")))
                .SetName("Add operation"),
            new TestCaseData(new DeleteOperation(Item.CreateTombStone("a")))
                .SetName("Delete operation"),  
        };
        [TestCaseSource(nameof(singleOperationTests))]
        public void TestSingleOperation(IOperation operation)
        {
            writer.Write(operation);
            using (var reader = GetReader())
            {
                IOperation result;
                reader.Read(out result).Should().BeTrue();
                result.Should().Be(operation);
                reader.Read(out result).Should().BeFalse();
            }
        }

        private static readonly TestCaseData[] manyOperationTests =
        {
            ParamsTestCaseData.Create<IOperation>(
                new AddOperation(Item.CreateItem("aa", "bbbb")), 
                new AddOperation(Item.CreateItem("bbbb", "c")), 
                new DeleteOperation(Item.CreateTombStone("aa"))),
            ParamsTestCaseData.Create<IOperation>(
                new DeleteOperation(Item.CreateTombStone("aa")), 
                new AddOperation(Item.CreateItem("b", "cccc")))
        };

        [TestCaseSource(nameof(manyOperationTests))]
        public void TestManyOperations(params IOperation[] operations)
        {
            foreach (var operation in operations)
                writer.Write(operation);
            using (var reader = GetReader())
            {
                IOperation result;
                foreach (var operation in operations)
                {
                    reader.Read(out result).Should().BeTrue();
                    result.Should().Be(operation);
                }
                reader.Read(out result).Should().BeFalse();
            }
        }

        [TestCaseSource(nameof(manyOperationTests))]
        public void TestManyOperations_Corrupted(params IOperation[] operations)
        {
            foreach (var operation in operations)
                writer.Write(operation);

            file.CorruptSuffix();
            repairer.RepairLog(file);

            using (var restoredReader = GetReader())
            {
                IOperation result;
                foreach (var operation in operations.Take(operations.Length - 1))
                {
                    restoredReader.Read(out result).Should().BeTrue();
                    result.Should().Be(operation);
                }
                restoredReader.Read(out result).Should().BeFalse();
            }
        }

        [TestCaseSource(nameof(manyOperationTests))]
        public void TestManyOperations_Corrupted_ThenContinue(params IOperation[] operations)
        {
            foreach (var operation in operations)
                writer.Write(operation);

            file.CorruptSuffix();
            repairer.RepairLog(file);

            var newOperation = new AddOperation(Item.CreateItem("11", "22"));
            using (var restoredWriter = GetWriter())
            { 
                restoredWriter.Write(newOperation);
            }
            using (var restoredReader = GetReader())
            {
                foreach (var operation in operations.Take(operations.Length - 1).Concat(new[] { newOperation }))
                {
                    IOperation result;
                    restoredReader.Read(out result).Should().BeTrue();
                    result.Should().Be(operation);
                }
            }
        }
    }
}
