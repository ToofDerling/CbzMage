using PdfConverter.Model;
using System.Collections.Generic;
using System.Linq;

namespace PdfConverter.PdfFlow
{
    public class PdfComic : Comic
    {
        public PdfComic(string path) : base(path)
        {
            OutputDirectory = System.IO.Path.ChangeExtension(path, null);
            OutputFile = System.IO.Path.ChangeExtension(path, FileExt.Cbz);
        }

        public static List<PdfComic> List(params string[] paths) => new List<PdfComic>(paths.Select(x => new PdfComic(x)));
    }
}
