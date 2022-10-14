using PdfConverter;
using PdfConverter.Model;
using System.Collections.Generic;
using System.Linq;

namespace PdfConverter.CbzFlow
{
    public class CbzComic : Comic
    {
        public CbzComic(string path) : base(path)
        {
            OutputDirectory = System.IO.Path.ChangeExtension(path, null);
            OutputFile = System.IO.Path.ChangeExtension(path, FileExt.Pdf);
        }

        public string[] Files { get; set; }

        public static List<CbzComic> List(params string[] paths) => new List<CbzComic>(paths.Select(x => new CbzComic(x)));
    }
}
