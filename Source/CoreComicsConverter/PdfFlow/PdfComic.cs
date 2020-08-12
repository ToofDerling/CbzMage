using CoreComicsConverter.Model;
using System.Collections.Generic;
using System.Linq;

namespace CoreComicsConverter.PdfFlow
{
    public class PdfComic : Comic
    {
        public PdfComic(string path) : base(ComicType.Pdf, path)
        {
            OutputDirectory = System.IO.Path.ChangeExtension(path, null);
            OutputFile = System.IO.Path.ChangeExtension(path, FileExt.Cbz);
        }
   
        public string GetSinglePagePngString(int pageNumber)
        {
            return $"{pageNumber.ToString().PadLeft(PageCountLength, '0')}-{1.ToString().PadLeft(PageCountLength, '0')}{FileExt.Png}";
        }
            
        public static List<PdfComic> List(params string[] paths) => new List<PdfComic>(paths.Select(x => new PdfComic(x)));
    }
}
