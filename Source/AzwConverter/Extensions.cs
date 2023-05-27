using CbzMage.Shared.Extensions;
using MobiMetadata;

namespace AzwConverter
{
    public static class Extensions
    {
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
