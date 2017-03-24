using NUnit.Framework;

namespace DataLayerTests
{
    public static class ParamsTestCaseData
    {
        public static TestCaseData Create<T>(params T[] items)
        {
            return new TestCaseData(items).SetName(string.Join(", ", items));
        }
    }
}