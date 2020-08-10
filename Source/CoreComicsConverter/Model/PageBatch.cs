using System.Collections.Generic;
using System.Linq;

namespace CoreComicsConverter.Model
{
    public class PageBatch 
    {
        public int Width { get; set; }

        public int Height { get; set; }

        /// <summary>
        /// Used by Pdf flow when reading pages using Ghostscript.
        /// </summary>
        public int Dpi { get; set; }

        /// <summary>
        /// The pages with this imagesize
        /// </summary>
        public List<Page> Pages { get; set; }

        public int FirstPage => Pages == null ? -1 : Pages.First().Number;

        public int LastPage => Pages == null ? -1 : Pages.Last().Number;

        public int NewWidth { get; set; }

        public int NewHeight { get; set; }
    }
}
