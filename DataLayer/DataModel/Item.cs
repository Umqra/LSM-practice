using System;
using System.IO;
using System.Text;

namespace DataLayer.DataModel
{
    public class Item : IComparable<Item>
    {
        public static Item CreateItem(string key, string value)
        {
            return new Item(key, value, false);
        }

        public static Item CreateTombStone(string key)
        {
            return new Item(key, null, true);
        }

        private Item(string key, string value, bool isTombstone)
        {
            Key = key;
            Value = value;
            IsTombStone = isTombstone;
        }

        public string Key { get; }

        public string Value { get; }

        public bool IsTombStone { get; }

        public override string ToString()
        {
            if (IsTombStone)
                return $"{nameof(Key)}: {Key}, deleted";
            return $"{nameof(Key)}: {Key}, {nameof(Value)}: {Value}";
        }

        #region EqualityMembers
        private bool Equals(Item other)
        {
            return string.Equals(Key, other.Key);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Item)obj);
        }

        public override int GetHashCode()
        {
            return Key?.GetHashCode() ?? 0;
        }

        public int CompareTo(Item other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return string.Compare(Key, other.Key, StringComparison.Ordinal);
        }

        #endregion
    }
}
