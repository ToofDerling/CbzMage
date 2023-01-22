using CbzMage.Shared.Extensions;
using MobiMetadata;
using System.Net;
using System.Text;

namespace AzwConverter
{
    public static class Extensions
    {
        private static readonly char[] _invalidChars = Path.GetInvalidFileNameChars();

        private const int _spaceChar = 32;

        public static string ToFileSystemString(this string str)
        {
            str = WebUtility.HtmlDecode(str);

            var sb = new StringBuilder(str);

            for (int i = 0, sz = sb.Length; i < sz; i++)
            {
                var ch = sb[i];
                if (_invalidChars.Contains(ch) || (ch != _spaceChar && char.IsWhiteSpace(ch)))
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

        public static bool IsAzwOrAzw3File(this FileInfo fileInfo)
        {
            return fileInfo.Name.IsAzwOrAzw3File();
        }

        public static bool IsAzwOrAzw3File(this string name)
        {
            return name.EndsWithIgnoreCase(".azw") || name.EndsWithIgnoreCase(".azw3");
        }

        public static bool IsAzwResOrAzw6File(this FileInfo fileInfo)
        {
            return fileInfo.Name.IsAzwResOrAzw6File();
        }

        public static bool IsAzwResOrAzw6File(this string name)
        {
            return name.EndsWithIgnoreCase(".azw.res") || name.EndsWithIgnoreCase(".azw6");
        }

        public static string GetFullTitle(this MobiHead mobiHeader)
        {
            var title = mobiHeader.ExthHeader.UpdatedTitle;
            if (string.IsNullOrWhiteSpace(title))
            {
                title = mobiHeader.FullName;
            }
            return title;
        }
    }
}
