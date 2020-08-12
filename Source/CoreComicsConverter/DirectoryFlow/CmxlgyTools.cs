using CoreComicsConverter.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CoreComicsConverter.DirectoryFlow
{
    public static class CmxlgyTools
    {
        private static readonly Regex downloadMatcher = new Regex(@$"^page\d+\{FileExt.Png}$");

        public static bool IsDownload(string[] paths)
        {
            int padLen = paths.Length.ToString().Length;
            var zeros = "0".PadLeft(padLen, '0');
            var pageZero = $"page{zeros}";

            var firstFile = Path.GetFileNameWithoutExtension(paths[0]);
            if (firstFile != pageZero)
            {
                return false;
            }

            var isDownload = paths.All(p => downloadMatcher.IsMatch(Path.GetFileName(p)));
            return isDownload;
        }

        public static int GetDownloadPageNumber(string name)
        {
            var numberStart = name.IndexOf("page") + 4;
            var numberEnd = name.IndexOf('.', numberStart);

            var numberString = name.Substring(numberStart, numberEnd - numberStart);

            var number = int.Parse(numberString);
            return number;
        }

        public static List<int> GetPagesMissing(string[] pages, out bool isMangaDownload)
        {
            var consecutivePages = 0;
            var pagesSkippingOnePage = 0;

            var pageRangesMissing = new List<int>();
            var singlePagesMissing = new List<int>();

            isMangaDownload = false;

            for (var i = 1; i < pages.Length; i++)
            {
                var pageNumber = GetDownloadPageNumber(pages[i]);
                var prevPageNumber = GetDownloadPageNumber(pages[i - 1]);

                switch (pageNumber - prevPageNumber)
                {
                    case 1:
                        consecutivePages++;
                        break;
                    case 2:
                        pagesSkippingOnePage++;
                        singlePagesMissing.Add(prevPageNumber + 1);
                        break;
                    default:
                        for (int missing = prevPageNumber + 1; missing <= pageNumber - 1; missing++)
                        {
                            pageRangesMissing.Add(missing);
                        }
                        break;
                }
            }

            if (pagesSkippingOnePage > consecutivePages)
            {
                isMangaDownload = true;

                if (pageRangesMissing.Any())
                {
                    var lastPageNumber = GetDownloadPageNumber(pages.Last());

                    for (var shouldExist = singlePagesMissing.First() + 1; shouldExist < lastPageNumber; shouldExist += 2)
                    {
                        if (pageRangesMissing.Contains(shouldExist) && pageRangesMissing.Contains(shouldExist - 1) && pageRangesMissing.Contains(shouldExist + 1))
                        {
                            pageRangesMissing.Remove(shouldExist - 1);
                            pageRangesMissing.Remove(shouldExist + 1);
                        }
                    }
                }

                singlePagesMissing.Clear();
            }

            return singlePagesMissing.Concat(pageRangesMissing).AsList();
        }
    }
}
