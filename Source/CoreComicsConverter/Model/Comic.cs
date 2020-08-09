using System;
using System.Collections.Generic;
using System.Text;

namespace CoreComicsConverter.Model
{
    public class Comic
    {
        public ComicType Type { get; private set; }

        public string Path { get; private set; }

        public int PageCount
        {
            get => _pageCount;
            set
            {
                _pageCount = value;
                PageCountLength = _pageCount.ToString().Length;
            }
        }

        private int _pageCount;

        public int PageCountLength { get; private set; }

        public int ImageCount { get; set; }

        public Comic(ComicType type, string path)
        {
            Type = type;
            Path = path;
        }

        public static List<Comic> List()
        {
            return new List<Comic>();
        }
    }
}
