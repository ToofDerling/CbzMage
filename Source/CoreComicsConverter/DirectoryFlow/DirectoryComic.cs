using CoreComicsConverter.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreComicsConverter.DirectoryFlow
{
    public class DirectoryComic : Comic
    {
        public DirectoryComic(string directory, string[] files) : base(ComicType.Directory, directory)
        {
            Files = files;
        }

        public string[] Files { get; private set; }

       

        public static List<DirectoryComic> List(string directory, string[] files)
        {
            return new List<DirectoryComic> { new DirectoryComic(directory, files) };
        }
    }
}
