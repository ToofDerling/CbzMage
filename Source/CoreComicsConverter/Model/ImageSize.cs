using System.Collections.Generic;

namespace CoreComicsConverter.Model
{
    public class ImageSize
    {
        public int Width { get; set; }

        public int Height { get; set; }

        /// <summary>
        /// Shortcut to the first pagenumber in Pages.
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Used by Pdf flow when reading pages using Ghostscript.
        /// </summary>
        public int Dpi { get; set; }

        /// <summary>
        /// The pages with this imagesize
        /// </summary>
        public List<int> Pages { get; set; }
    }
}
