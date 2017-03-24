using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.OperationLog.Operations;

namespace DataLayer.DataModel
{
    public interface IItemSerializer
    {
        byte[] Serialize(Item item);
        Item Deserialize(Stream stream);
    }

    class ItemSerializer : IItemSerializer
    {
        public byte[] Serialize(Item item)
        {
            using (var stream = new MemoryStream())
            using (var binaryWriter = new BinaryWriter(stream))
            {
                binaryWriter.Write(BitConverter.GetBytes(item.Key.Length));
                binaryWriter.Write(item.IsTombStone
                    ? BitConverter.GetBytes(-1)
                    : BitConverter.GetBytes(item.Value.Length));
                binaryWriter.Write(Encoding.UTF8.GetBytes(item.Key));
                if (!item.IsTombStone)
                    binaryWriter.Write(Encoding.UTF8.GetBytes(item.Value));
                return stream.ToArray();
            }
        }

        public Item Deserialize(Stream stream)
        {
            var binaryReader = new BinaryReader(stream);
            int keyLength = binaryReader.ReadInt32();
            int valueLength = binaryReader.ReadInt32();
            var keyBytes = binaryReader.ReadExactly(keyLength);
            if (valueLength != -1)
            {
                var valueBytes = binaryReader.ReadExactly(valueLength);
                return Item.CreateItem(Encoding.UTF8.GetString(keyBytes), Encoding.UTF8.GetString(valueBytes));
            }
            return Item.CreateTombStone(Encoding.UTF8.GetString(keyBytes));
        }
    }
}
