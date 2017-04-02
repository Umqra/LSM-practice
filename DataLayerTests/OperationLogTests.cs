using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using DataLayer.DataModel;
using DataLayer.MemoryCache;
using DataLayer.OperationLog;
using DataLayer.OperationLog.Operations;
using FluentAssertions;
using NUnit.Framework;

namespace DataLayerTests
{
    [TestFixture]
    class OperationLogTests
    {
        private FileInfoBase file;
        private IOperationLogRepairer repairer;
        private CacheManager cacheManager;
        [SetUp]
        public void Setup()
        {
            var filePath = @"c:\log.txt";
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [filePath] = new MockFileData("")
            });
            file = new MockFileInfo(fileSystem, filePath);
            repairer = new OperationLogRepairer();
        }

        private void CorruptFile(FileInfoBase fileToCorrupt, int brokenBytes = 2)
        {
            var length = fileToCorrupt.Length;
            using (var stream = fileToCorrupt.OpenWrite())
                stream.SetLength(length - brokenBytes);
        }

        private IOperationLogReader GetReader()
        {
            return new OperationLogReader(file.Open(FileMode.OpenOrCreate, FileAccess.Read), new OperationSerializer());
        }

        private IOperationLogWriter GetWriter()
        {
            return new OperationLogWriter(file.Open(FileMode.Append, FileAccess.Write), new OperationSerializer());
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
            using (var writer = GetWriter())
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
            using (var writer = GetWriter())
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
            using (var writer = GetWriter())
                foreach (var operation in operations)
                    writer.Write(operation);

            CorruptFile(file);
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
            using (var writer = GetWriter())
                foreach (var operation in operations)
                    writer.Write(operation);

            CorruptFile(file);
            repairer.RepairLog(file);

            var newOperation = new AddOperation(Item.CreateItem("11", "22"));
            using (var restoredWriter = GetWriter())
                restoredWriter.Write(newOperation);

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
