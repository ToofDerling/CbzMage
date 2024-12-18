namespace CbzMage.Shared.Extensions
{
    public static class EnumerableExtensions
    {
        // Stolen from Dapper SqlMapper.cs
        public static List<T> AsList<T>(this IEnumerable<T> source) => source == null || source is List<T> ? (List<T>)source : source.ToList();

        public static string SIfNot1(this int count) => count != 1 ? "s" : string.Empty;

        public static string SIfNot1<T>(this IEnumerable<T> enu) => enu.Count().SIfNot1();
    }
}

