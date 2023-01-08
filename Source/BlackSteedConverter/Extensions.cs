using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSteedConverter
{
    internal static class Extensions
    {
        public static bool IsBookDirectory(this string str)
        {
            return IsGuidInFormat(str, "N");
        }

        public static bool IsBookFile(this string str)
        {
            return IsGuidInFormat(str, "D");
        }

        private static bool IsGuidInFormat(string str, string format)
        {
            return Guid.TryParseExact(str, format, out var _);
        }
    }
}
