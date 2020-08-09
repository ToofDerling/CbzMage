using System.Collections.Generic;
using System.Linq;

namespace CoreComicsConverter.Model
{
    public class PageBatch : Page
    {
        /// <summary>
        /// Used by Pdf flow when reading pages using Ghostscript.
        /// </summary>
        public int Dpi { get; set; }

        /// <summary>
        /// The pages with this imagesize
        /// </summary>
        public List<int> PageNumbers { get; set; }

        public int FirstPage => PageNumbers == null ? -1 : PageNumbers.First();

        public int LastPage => PageNumbers == null ? -1 : PageNumbers.Last();

        public int NewWidth { get; set; }

        public int NewHeight { get; set; }
    }
}
