using System.IO;

namespace DataLayer.OperationLog.Operations
{
    public static class StreamExtensions
    {
        public static byte[] ReadExactly(this BinaryReader stream, int count)
        {
            byte[] bytes = stream.ReadBytes(count);
            if (bytes.Length < count)
                throw new EndOfStreamException();
            return bytes;
        }
    }
}