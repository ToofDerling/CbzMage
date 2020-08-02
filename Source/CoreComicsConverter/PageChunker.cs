using System.Collections.Generic;

namespace Rotvel.PdfConverter
{
    public class PageChunker
    {
        public List<int>[] CreatePageLists(int numberOfPages, int startPage, int numberOfChunks)
        {
            var pageLists = new List<int>[numberOfChunks];

            for (var i = 0; i < numberOfChunks; i++)
            {
                pageLists[i] = new List<int>();
            }

            for (var page = startPage; page <= numberOfPages; page++)
            {
                var index = page % numberOfChunks;

                pageLists[index].Add(page);
            }

            return pageLists;
        }
    }
}
