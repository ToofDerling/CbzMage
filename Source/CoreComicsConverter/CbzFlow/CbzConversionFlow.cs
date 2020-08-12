using CoreComicsConverter.DirectoryFlow;
using CoreComicsConverter.Extensions;
using CoreComicsConverter.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CoreComicsConverter.CbzFlow
{
    public class CbzConversionFlow
    {
        public void ExtractCbz(CbzComic cbzComic)
        {
            cbzComic.CreateOutputDirectory();

            var sevenZip = new SevenZipMachine();

            sevenZip.PageCompressed += (s, e) => Console.WriteLine(e.Page.Name);

            sevenZip.ExtractFile(cbzComic);
        }
    }
}
