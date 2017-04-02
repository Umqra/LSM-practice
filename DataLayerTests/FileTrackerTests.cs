using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.Utilities;
using FluentAssertions;
using NUnit.Framework;

namespace DataLayerTests
{
    [TestFixture]
    class FileTrackerTests
    {
        public DirectoryInfoBase CreateDirectory(params string[] files)
        {
            var filesDictionary = files.ToDictionary(file => file, file => new MockFileData(""));
            var fileSystem = new MockFileSystem(filesDictionary);
            return new MockDirectoryInfo(fileSystem, ".");
        }

        [TestCase("log-1.txt")]
        [TestCase("{}-{}.txt")]
        [TestCase("{0}-{0}.txt")]
        public void TestIncorrectFormatString(string format)
        {
            Action trackerFactory = () => new FileTracker(format, CreateDirectory());
            trackerFactory.ShouldThrow<ArgumentException>();
        }

        [TestCase("log-{0}.txt")]
        [TestCase("{0}")]
        [TestCase("very-long-and-complex-name-v1.2-{0}.tar.gz")]
        public void TestCorrectFormatString(string format)
        {
            new FileTracker(format, CreateDirectory());
        }

        [Test]
        public void TestFileEnumeration()
        {
            var directory = CreateDirectory("log-1.txt", "log-3.txt", "readme.txt", "log-0.old");
            var fileTracker = new FileTracker("log-{0}.txt", directory);
            fileTracker.Files.Select(f => f.Name).Should().BeEquivalentTo("log-1.txt", "log-3.txt");
        }

        [Test]
        public void TestNewFileCreation()
        {
            var directory = CreateDirectory("log-1.txt", "log-3.txt", "readme.txt", "log-0.old");
            var fileTracker = new FileTracker("log-{0}.txt", directory);
            fileTracker.CreateNewFile().Name.Should().Be("log-4.txt");
        }
    }
}
