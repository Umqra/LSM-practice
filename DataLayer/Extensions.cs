using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    public static class Extensions
    {
        public static IEnumerable<IEnumerable<T>> GroupBySize<T>(this IEnumerable<T> data, int size)
        {
            return data
                .Select((item, index) => new {Item = item, Index = index})
                .GroupBy(t => t.Index / size, t => t.Item);
        }

        public static IEnumerable<T> MergeWith<T>(this IEnumerable<T> first, IEnumerable<T> second)
            where T:IComparable<T>
        {
            using (var enumerator = first.GetEnumerator())
            {
                bool firstContinue = enumerator.MoveNext();
                foreach (var secondItem in second)
                {
                    while (firstContinue && enumerator.Current.CompareTo(secondItem) <= 0)
                    {
                        yield return enumerator.Current;
                        firstContinue &= enumerator.MoveNext();
                    }
                    yield return secondItem;
                }
                while (firstContinue)
                {
                    yield return enumerator.Current;
                    firstContinue &= enumerator.MoveNext();
                }
            }
        }

        public static bool LessThan(this string a, string b)
        {
            return string.Compare(a, b, StringComparison.Ordinal) < 0;
        }

        public static async Task<int> ReadIntAsync(this Stream stream)
        {
            byte[] buffer = new byte[4];
            await stream.ReadAsync(buffer, 0, 4);
            return BitConverter.ToInt32(buffer, 0);
        }

        public static async Task<long> ReadLongAsync(this Stream stream)
        {
            byte[] buffer = new byte[8];
            await stream.ReadAsync(buffer, 0, 8);
            return BitConverter.ToInt64(buffer, 0);
        }

        public static async Task<string> ReadStringAsync(this Stream stream, int length, Encoding encoding)
        {
            byte[] buffer = new byte[length];
            await stream.ReadAsync(buffer, 0, length);
            return encoding.GetString(buffer);
        }
    }
}