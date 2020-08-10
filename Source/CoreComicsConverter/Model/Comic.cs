using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace CoreComicsConverter.Model
{
    public class Comic
    {
        public ComicType Type { get; private set; }

        public Comic(ComicType type, string path)
        {
            Type = type;
            Path = path;
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

        public void CreateOutputFile()
        {
            File.Delete(OutputFile);

            ZipFile.CreateFromDirectory(OutputDirectory, OutputFile, CompressionLevel.Optimal, includeBaseDirectory: false);
        }

        public virtual void CleanOutputDirectory()
        {
            //nop
        }

        public static List<Comic> List()
        {
            return new List<Comic>();
        }
    }
}
