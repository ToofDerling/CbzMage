using System;
using System.Collections.Generic;
using System.Text;

namespace CoreComicsConverter.Model
{
    public class DirectoryComic : Comic
    {



        public DirectoryComic(string directory) : base(ComicType.Directory, directory)
        { 
        }
    }
}
