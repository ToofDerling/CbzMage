using CoreComicsConverter.DirectoryFlow;
using System.Collections.Generic;
using System.Linq;

namespace CoreComicsConverter.CbzCbrFlow
{
    public class CbzComic : DirectoryComic
    {
        public CbzComic(string path) : base(path)
        {
            OutputDirectory = System.IO.Path.ChangeExtension(path, null);
            OutputFile = System.IO.Path.ChangeExtension(path, FileExt.Pdf);
        }

        public static List<CbzComic> List(params string[] paths) => new List<CbzComic>(paths.Select(x => new CbzComic(x)));
    }
}
