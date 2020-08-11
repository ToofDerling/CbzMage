using System.Collections.Generic;
using System.IO;

namespace CoreComicsConverter.Model
{
    public class Comic
    {
        public ComicType Type { get; private set; }

        public Comic(ComicType type, string path)
        {
            Type = type;
            Path = System.IO.Path.GetFullPath(path);
        }

        public string Path { get; private set; }

        public string OutputDirectory { get; protected set; }

        public string OutputFile { get; protected set; }

        public bool OutputFileCreated { get; set; }

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
        
        public string GetJpgPageString(int pageNumber)
        {
            return GetPageString(pageNumber, "jpg");
        }

        protected string GetPageString(int pageNumber, string extension)
        {
            var page = pageNumber.ToString().PadLeft(PageCountLength, '0');
            return $"page-{page}.{extension}";
        }
        
        public void CleanOutputDirectory()
        {
            if (Directory.Exists(OutputDirectory))
            {
                Directory.Delete(OutputDirectory, recursive: true);
            }
        }

        public void CreateOutputDirectory()
        {
            CleanOutputDirectory();

            Directory.CreateDirectory(OutputDirectory);
        }

        public static List<Comic> List()
        {
            return new List<Comic>();
        }
    }
}
