using System.Collections.Generic;
using System.Linq;

namespace PdfConverter
{
    public class Pdf
    {
        public Pdf(string path)
        {
            Path = path;
        }

        public string Path { get; private set; }

        public int PageCount
        {
            get => _pageCount;
            set
            {
                _pageCount = value;
                _pageCountLength = _pageCount.ToString().Length;
            }
        }

        private int _pageCount;
        private int _pageCountLength;

        public int ImageCount { get; set; }

        public string GetPageString(int pageNumber)
        {
            var page = pageNumber.ToString().PadLeft(_pageCountLength, '0');
            return $"page-{page}.jpg";
        }

        public static List<Pdf> List(params string[] paths)
        {
            return new List<Pdf>(paths.Select(x => new Pdf(x)));
        }
    }
}
