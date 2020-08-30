using CoreComicsConverter.Model;
using System.Collections.Generic;

namespace CoreComicsConverter.DirectoryFlow
{
    public class DirectoryComic : Comic
    {
        protected DirectoryComic(string directory) : base(directory)
        {
        }

        public DirectoryComic(string directory, string[] files) : this(directory)
        {
            Files = files;
            IsDownload = CmxlgyTools.IsDownload(files);

            OutputDirectory = $"{directory}.tmp";

            OutputFile = $"{System.IO.Path.TrimEndingDirectorySeparator(directory)}{FileExt.Cbz}";
        }

        public string[] Files { get; set; }

        public bool IsDownload { get; }

        public override string GetJpgPageString(int pageNumber)
        {
            var pageBase = IsDownload ? "page" : "page-";

            return GetPageString(pageBase, pageNumber, FileExt.Jpg);
        }

        public static List<DirectoryComic> List(string directory, string[] files) => new List<DirectoryComic> { new DirectoryComic(directory, files) };
    }
}
