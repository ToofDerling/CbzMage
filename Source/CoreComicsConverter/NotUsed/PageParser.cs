using CoreComicsConverter.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CoreComicsConverter.Images
{
    public class PageParser
    {
        private static Dictionary<string, (int width, int height, int count)> BuildImageSizesMap(ConcurrentBag<(string page, int width, int height)> imageSizes)
        {
            ILookup<string, (string page, int width, int height)> lookup = imageSizes.ToLookup(i => i.page);

            var imageSizesMap = new Dictionary<string, (int, int, int count)>();

/*            foreach (var (width, height) in imageSizes)
            {
                var key = $"{width} x {height}";

                var count = imageSizesMap.TryGetValue(key, out var existingImageSize) ? existingImageSize.count + 1 : 1;

                imageSizesMap[key] = (width, height, count);
            }
*/
            return imageSizesMap;
        }

        public event EventHandler<PageEventArgs> PageParsed;
    }
}
