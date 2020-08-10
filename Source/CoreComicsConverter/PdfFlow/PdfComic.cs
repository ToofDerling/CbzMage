using CoreComicsConverter.Model;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace CoreComicsConverter.PdfFlow
{
    public class PdfComic : Comic
    {
        public PdfComic(string path) : base(ComicType.Pdf, path)
        {
            OutputDirectory = System.IO.Path.ChangeExtension(path, null);
            OutputFile = System.IO.Path.ChangeExtension(path, ".cbz");
        }

        //public string GetPngPageString(int pageNumber)
        //{
        //    return GetPageString(pageNumber, "png");
        //}

        public string GetSinglePagePngString(int pageNumber)
        {
            return $"{pageNumber.ToString().PadLeft(PageCountLength, '0')}-{1.ToString().PadLeft(PageCountLength, '0')}.png";
        }
        
        public void ExtractPages(string cbzFile)
        {
            CleanOutputDirectory();

            ZipFile.ExtractToDirectory(cbzFile, OutputDirectory);
        }

        public override void CleanOutputDirectory()
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
