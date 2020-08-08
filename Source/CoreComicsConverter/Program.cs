using CoreComicsConverter.Images;
using CoreComicsConverter.Model;
using CoreComicsConverter.PdfFlow;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CoreComicsConverter
{
    public static class Program
    {

#if DEBUG
        //private const string _testPdf = @"D:\Data\Pdf\Test\Hawkworld New Edition - Timothy Truman";
        private const string _testPdf = @"D:\Data\Pdf\Test\";
#else
        private const string _testPdf = null;
#endif

        public static void Main(string[] args)
        {
            
            //var comic = new DirectoryComic(_testPdf);
            ////pdfComic.ExtractPages(_testPdf);

            //var dconverter = new PdfComicConverter();
            //dconverter.ConvertToCbz(comic, null);

            //var cbzConverter = new ArchiveConverter();
            //cbzConverter.ConvertToPdf(comic);
         
            var pdfList = InitializePdfPath(args);

            if (pdfList.Any())
            {
                CompressCbzTask compressCbzTask = null;
                var converter = new ComicConverter();

                pdfList.ForEach(pdf =>
                {
                    compressCbzTask = ConvertPdf(pdf, converter, compressCbzTask);
                });

                converter.WaitForCompressPages(compressCbzTask);
            }
            else
            {
                Console.WriteLine("CoreComicsConverter <directory|pdf_file>");
            }

            Console.ReadLine();
        }

        private static CompressCbzTask ConvertPdf(PdfComic pdfComic, ComicConverter converter, CompressCbzTask compressPdfTask)
        {
            var stopWatch = Stopwatch.StartNew();

            compressPdfTask = converter.ConvertToCbz(pdfComic, compressPdfTask);

            stopWatch.Stop();
            var passed = stopWatch.Elapsed;

            Console.WriteLine($"{passed.Minutes} min {passed.Seconds} sec");
            Console.WriteLine();

            return compressPdfTask;
        }

        private static List<PdfComic> InitializePdfPath(string[] args)
        {
            var path = (string)null;

            if (!string.IsNullOrEmpty(_testPdf))
            {
                path = _testPdf;
            }
            else if (args.Length > 0)
            {
                path = args[0];
            }

            if (!string.IsNullOrEmpty(path))
            {
                if (Directory.Exists(path))
                {
                    var files = Directory.GetFiles(path, "*.pdf"); // Search pattern is case insensitive
                    if (files.Length > 0)
                    {
                        return PdfComic.List(files.ToArray());
                    }
                }
                else if (File.Exists(path) && path.EndsWith(".pdf", StringComparison.InvariantCultureIgnoreCase))
                {
                    return PdfComic.List(path);
                }
            }

            // Nothing to do
            return PdfComic.List();
        }
    }
}
