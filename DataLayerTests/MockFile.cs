using System;
using System.IO;
using System.Linq;
using DataLayer.Utilities;

namespace DataLayerTests
{
    public class MockFile : IFileData
    {
        private MemoryStream stream;

        public MockFile()
        {
            stream = new MemoryStream();
        }
        public Stream GetStream(FileMode mode, FileAccess accessMode)
        {
            ReinitializeStream(b => b);
            if (mode == FileMode.Append)
                stream.Seek(0, SeekOrigin.End);
            return stream;
        }

        private void ReinitializeStream(Func<byte[], byte[]> transformData)
        {
            stream.Flush();

            var data = stream.ToArray();
            stream = new MemoryStream();
            data = transformData(data);
            stream.Write(data, 0, data.Length);
            stream.Position = 0;
        }

        public void CorruptSuffix(int brokenBytes = 2)
        {
            ReinitializeStream(b => b.Take(b.Length - brokenBytes).ToArray());
        }

        public string Path { get; set; }
    }
}