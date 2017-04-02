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
            }
        }

        public static bool LessThan(this string a, string b)
        {
            return string.Compare(a, b, StringComparison.Ordinal) < 0;
        }
    }
}