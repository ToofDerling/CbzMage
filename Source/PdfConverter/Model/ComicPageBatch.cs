using System.Collections.Generic;

namespace PdfConverter.Model
{
    public class ComicPageBatch
    {
        public string BatchId { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        /// <summary>
        /// Used by Pdf flow when reading pages using Ghostscript.
        /// </summary>
        public int Dpi { get; set; }

        /// <summary>
        /// The pages with this imagesize
        /// </summary>
        public List<ComicPage> Pages { get; set; }

        public int NewWidth { get; set; }

        public int NewHeight { get; set; }
    }
}
