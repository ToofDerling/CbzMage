namespace PdfConverter
{
    public class PageChunker
    {
        public List<int>[] CreatePageLists(int numberOfPages, int numberOfChunks)
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
    }
}
