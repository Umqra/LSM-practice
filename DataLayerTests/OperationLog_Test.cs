using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.DataModel;
using DataLayer.OperationLog;
using DataLayer.OperationLog.Operations;
using DataLayer.Utilities;
using FluentAssertions;
using NUnit.Framework;

namespace DataLayerTests
{
    public class MockFile : IFile
    {
        private MemoryStream stream;

        public MockFile()
        {
            stream = new MemoryStream();
        }
        public Stream GetStream(FileAccess accessMode)
        {
            stream.Flush();
            stream.Position = 0;
            return stream;
        }

        public void CorruptSuffix(int brokenBytes = 2)
        {
            var data = stream.ToArray();
            stream = new MemoryStream(data.Take(data.Length - brokenBytes).ToArray());
        }
    }

    public static class ParamsTestCaseData
    {
        public static TestCaseData Create<T>(params T[] items)
        {
            return new TestCaseData(items).SetName(string.Join(", ", items));
        }
    }

    [TestFixture]
    class OperationLogTests
    {
        private MockFile file;
        private OpLogManager manager;
        [SetUp]
        public void Setup()
        {
            file = new MockFile();
            manager = new OpLogManager(file, new OperationSerializer());
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
            manager.Write(operation);
            IOperation result;
            manager.Read(out result).Should().BeTrue();
            result.Should().Be(operation);
            manager.Read(out result).Should().BeFalse();
        }

        private static readonly TestCaseData[] manyOperationTests =
        {
            ParamsTestCaseData.Create<IOperation>(
                new AddOperation(Item.CreateItem("a", "b")), 
                new AddOperation(Item.CreateItem("b", "c")), 
                new DeleteOperation(Item.CreateTombStone("a"))),
            ParamsTestCaseData.Create<IOperation>(
                new DeleteOperation(Item.CreateTombStone("a")), 
                new AddOperation(Item.CreateItem("b", "c")))
        };

        [TestCaseSource(nameof(manyOperationTests))]
        public void TestManyOperations(params IOperation[] operations)
        {
            foreach (var operation in operations)
                manager.Write(operation);
            IOperation result;
            foreach (var operation in operations)
            {
                manager.Read(out result).Should().BeTrue();
                result.Should().Be(operation);
            }
            manager.Read(out result).Should().BeFalse();
        }

        [TestCaseSource(nameof(manyOperationTests))]
        public void TestManyOperations_Corrupted(params IOperation[] operations)
        {
            foreach (var operation in operations)
                manager.Write(operation);
            file.CorruptSuffix();

            IOperation result;
            foreach (var operation in operations.Take(operations.Length - 1))
            {
                manager.Read(out result).Should().BeTrue();
                result.Should().Be(operation);
            }
            manager.Read(out result).Should().BeFalse();
        }
    }
}
