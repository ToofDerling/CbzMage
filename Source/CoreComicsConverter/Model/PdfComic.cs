using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace CoreComicsConverter.Model
{
    public class PdfComic : Comic
    {
        public PdfComic(string path) : base(ComicType.Pdf, path)
        {
            OutputDirectory = System.IO.Path.ChangeExtension(path, null);
        }

        public string OutputDirectory { get; private set; }

        public string GetJpgPageString(int pageNumber)
        {
            return GetPageString(pageNumber, "jpg");
        }

        public string GetPngPageString(int pageNumber)
        {
            return GetPageString(pageNumber, "png");
        }

        public string GetSinglePagePngString(int pageNumber)
        {
            return $"{pageNumber.ToString().PadLeft(PageCountLength, '0')}-{1.ToString().PadLeft(PageCountLength, '0')}.png";
        }

        private string GetPageString(int pageNumber, string extension)
        {
            var page = pageNumber.ToString().PadLeft(PageCountLength, '0');
            return $"page-{page}.{extension}";
        }

        public string GetCbzName()
        {
            return System.IO.Path.GetFileName(GetCbzPath());
        }

        public string GetCbzPath()
        {
            return System.IO.Path.ChangeExtension(Path, ".cbz");
        }

        public void CompressPages()
        {
            var cbzPath = GetCbzPath();
            File.Delete(cbzPath);

            ZipFile.CreateFromDirectory(OutputDirectory, cbzPath, CompressionLevel.Optimal, includeBaseDirectory: false);
        }

        public void ExtractPages(string cbzFile)
        {
            CleanOutputDirectory();
            ZipFile.ExtractToDirectory(cbzFile, OutputDirectory);
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

        public static List<PdfComic> List(params string[] paths)
        {
            return new List<PdfComic>(paths.Select(x => new PdfComic(x)));
        }
    }
}
