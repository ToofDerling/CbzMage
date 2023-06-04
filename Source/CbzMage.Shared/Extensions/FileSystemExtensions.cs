using System.Net;
using System.Text;

namespace CbzMage.Shared.Extensions
{
    public static class FilesystemExtensions
    {
        private static readonly char[] _invalidChars = Path.GetInvalidFileNameChars().Union(Path.GetInvalidPathChars()).ToArray();

        private const int _spaceChar = 32;

        public static string ToFileSystemString(this string str)
        {
            str = WebUtility.HtmlDecode(str);

            var sb = new StringBuilder(str);

            for (int i = 0, sz = sb.Length; i < sz; i++)
            {
                var ch = sb[i];
                if (_invalidChars.Contains(ch) || ch != _spaceChar && char.IsWhiteSpace(ch))
                {
                    sb[i] = ' ';
                }
            }

            return sb.Replace("   ", " ").Replace("  ", " ").ToString().Trim();
        }

        public static bool IsDirectory(this FileSystemInfo e) => (e.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
    }
}
