using System.Reflection.Metadata;
using System.Text;

namespace AzwConverter
{
    public static class Extensions
    {
        public static string ToFileSystemString(this string str)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(str);

            for (int i = 0, sz = sb.Length; i < sz; i++)
            {   
                var ch = sb[i];
                if (invalidChars.Contains(ch) || (char.IsWhiteSpace(ch) && (int)ch != 32))
                {
                    sb[i] = ' ';
                }
            }

            return sb.Replace("   ", " ").Replace("  ", " ").ToString().Trim();
        }

        public static string RemoveAllMarkers(this string name)
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

        public static string SIf1(this int count)
        {
            return count != 1 ? "s" : string.Empty;
        }

        public static string SIf1<T>(this IEnumerable<T> enu)
        {
            return enu.Count().SIf1();
        }

        public static void CreateDirIfNotExists(this string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
    }
}
