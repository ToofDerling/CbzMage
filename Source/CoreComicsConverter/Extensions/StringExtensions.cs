using System;

namespace CoreComicsConverter.Extensions
{
    public static class StringExtensions
    {
        public static bool EqualsIgnoreCase(this string a, string b)
        {
            return string.Compare(a, b, true) == 0;
        }

        public static bool EndsWithIgnoreCase(this string a, string endsWith)
        {
            return a.EndsWith(endsWith, StringComparison.OrdinalIgnoreCase);
        }
    }
}
