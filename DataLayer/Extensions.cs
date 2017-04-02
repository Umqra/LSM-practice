using System;
using System.Collections.Generic;
using System.Linq;

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
        {
            using (var firstEnumerator = first.GetEnumerator())
            using (var secondEnumerator = second.GetEnumerator())
            {
                throw new NotImplementedException();        
            }
        }

        public static bool LessThan(this string a, string b)
        {
            return string.Compare(a, b, StringComparison.Ordinal) < 0;
        }
    }
}