using System.Collections.Generic;
using System.Linq;

namespace CoreComicsConverter.Extensions
{
    public static class EnumerableExtensions
    {
        // Stolen from Dapper SqlMapper.cs
        public static List<T> AsList<T>(this IEnumerable<T> source) => source == null || source is List<T> ? (List<T>)source : source.ToList();
    }
}

