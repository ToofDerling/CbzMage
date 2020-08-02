using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace CoreComicsConverter
{
    public class Pdf
    {
        public Pdf(string path)
        {
            PdfPath = path;
            OutputDirectory = Path.ChangeExtension(path, null);
        }

        public string PdfPath { get; private set; }

        public string OutputDirectory { get; private set; }

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

        public string GetPngPageString(int pageNumber)
        {
            return GetPageString(pageNumber, "png");
        }

        private string GetPageString(int pageNumber, string extension)
        {
            var page = pageNumber.ToString().PadLeft(PageCountLength, '0');
            return $"page-{page}.{extension}";
        }

        public string GetCbzName()
        {
            return Path.GetFileName(GetCbzPath());
        }

        public string GetCbzPath()
        {
            return Path.ChangeExtension(PdfPath, ".cbz");
        }

        public void CompressPages()
        {
            var cbzPath = GetCbzPath();
            File.Delete(cbzPath);

            ZipFile.CreateFromDirectory(OutputDirectory, cbzPath, CompressionLevel.Optimal, includeBaseDirectory: false);
        }

        public bool CbzFileCreated { get; set; }

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

        public static List<Pdf> List(params string[] paths)
        {
            return new List<Pdf>(paths.Select(x => new Pdf(x)));
        }
    }
}
