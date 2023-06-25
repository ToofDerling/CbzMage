namespace PdfConverter
{
    public class PageChunker
    {
        public static List<int>[] CreatePageLists(int numberOfPages, int numberOfChunks)
        {
            var pageLists = new List<int>[numberOfChunks];

            for (var i = 0; i < numberOfChunks; i++)
            {
                pageLists[i] = new List<int>();
            }

            for (var i = 0; i < numberOfPages; i++)
            {
                var page = i + 1;
                var index = i % numberOfChunks;

                pageLists[index].Add(page);
            }

            return pageLists;
        }

        public static List<int>[] CreatePageRanges(int numberOfPages, int numberOfChunks)
        {
            var chunkSize = numberOfPages / numberOfChunks;
            var restChunk = numberOfPages % numberOfChunks;

            var chunkSizes = new int[numberOfChunks];

            for (var i = 0; i < numberOfChunks; i++)
            {
                chunkSizes[i] = chunkSize;
            }
            for (var i = 0; i < restChunk; i++)
            {
                chunkSizes[i]++;
            }

            var pageRanges = new List<int>[numberOfChunks];

            var pageNumber = 1;
            for (var i = 0; i < numberOfChunks; i++)
            {
                var pageCount = chunkSizes[i];
                var pageRange = new List<int>(pageCount);

                for (var j = 0; j < pageCount; j++)
                {
                    pageRange.Add(pageNumber);
                    pageNumber++;
                }

                pageRanges[i] = pageRange;
            }

            return pageRanges;
        }
    }
}
