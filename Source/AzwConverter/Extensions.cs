using System.Net;
using System.Text;

namespace AzwConverter
{
    public static class Extensions
    {
        private static readonly char[] _invalidChars = Path.GetInvalidFileNameChars();

        private const int _spaceCh = 32;

        public static string ToFileSystemString(this string str)
        {
            str = WebUtility.HtmlDecode(str);

            var sb = new StringBuilder(str);

            for (int i = 0, sz = sb.Length; i < sz; i++)
            {   
                var ch = sb[i];
                if (_invalidChars.Contains(ch) || (ch != _spaceCh && char.IsWhiteSpace(ch)))
                {
                    sb[i] = ' ';
                }
            }

            return sb.Replace("   ", " ").Replace("  ", " ").ToString().Trim();
        }

        public static string RemoveAnyMarker(this string name)
        {
            foreach (var marker in Settings.AllMarkers)
            {
                if (name.StartsWith(marker))
                {
                    return name.Replace(marker, null).Trim();
                }
            }
            return name;
        }

        public static string AddMarker(this string name, string marker) => !name.StartsWith(marker) ? $"{marker} {name}" : name;

        public static string SIf1(this int count) => count != 1 ? "s" : string.Empty;

        public static string SIf1<T>(this IEnumerable<T> enu) => enu.Count().SIf1();

        public static bool IsAzwFile(this FileInfo fileInfo) => fileInfo.FullName.IsAzwFile();
        
        public static bool IsAzwFile(this string file) => file.EndsWith(Settings.AzwExt);

        public static bool IsAzwResFile(this FileInfo fileInfo) => fileInfo.FullName.EndsWith(Settings.AzwResExt);
    }
}
