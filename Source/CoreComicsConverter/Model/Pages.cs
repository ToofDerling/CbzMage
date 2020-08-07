using System.Collections.Generic;

namespace CoreComicsConverter.Model
{
    public class Pages
    {
        public int Width { get; set; }

        public int Height { get; set; }

        /// <summary>
        /// Shortcut to the first pagenumber in PageNumbers.
        /// </summary>
        public int FirstPageNumber { get; set; }

        /// <summary>
        /// Used by Pdf flow when reading pages using Ghostscript.
        /// </summary>
        public int Dpi { get; set; }

        public int LowerDpi { get; set; }

        /// <summary>
        /// The pages with this imagesize
        /// </summary>
        public List<int> PageNumbers { get; set; }
    }
}
