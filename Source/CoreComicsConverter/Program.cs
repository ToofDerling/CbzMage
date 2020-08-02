using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CoreComicsConverter
{
    public class Program
    {
        public static class QualityConstants
        {
            public const int MinimumDpi = 300;

            public const int JpegQuality = 98;

            public const int MaxHeightThreshold = 3100;

            public const int MaxHeight = 3056;
        }

#if DEBUG
        private static readonly string _testPdf = @"D:\Data\Pdf\Test";
#else
        private static readonly string _testPdf = null;
#endif

        public static readonly int ParallelThreads = Environment.ProcessorCount;

        public static readonly string GhostscriptPath = @"C:\Program Files\gs\gs9.52\bin\gswin64c.exe";

        public static void Main(string[] args)
        {
            var pdfList = InitializePdfPath(args);

            if (pdfList.Any())
            {
                CompressCbzTask compressCbzTask = null;
                var converter = new PdfComicConverter();

                pdfList.ForEach(pdf =>
                {
                    compressCbzTask = ConvertPdf(pdf, converter, compressCbzTask);
                });

                converter.WaitForCompressPages(compressCbzTask);
            }
            else
            {
                Console.WriteLine("PdfConverter <directory|pdf_file>");
            }

            Console.ReadLine();
        }

        private static CompressCbzTask ConvertPdf(Pdf pdf, PdfComicConverter converter, CompressCbzTask compressPdfTask)
        {
            var stopWatch = Stopwatch.StartNew();

            var pdfParser = new PdfParser();
            pdfParser.SetPageCount(pdf);

            Console.WriteLine(pdf.PdfPath);
            Console.WriteLine($"{pdf.PageCount} pages");

            compressPdfTask = converter.ConvertToCbz(pdf, compressPdfTask);

            stopWatch.Stop();
            var passed = stopWatch.Elapsed;

            Console.WriteLine($"{passed.Minutes} min {passed.Seconds} sec");
            Console.WriteLine();

            return compressPdfTask;
        }

        private static List<Pdf> InitializePdfPath(string[] args)
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
                        return Pdf.List(files.ToArray());
                    }
                }
                else if (File.Exists(path) && path.EndsWith(".pdf", StringComparison.InvariantCultureIgnoreCase))
                {
                    return Pdf.List(path);
                }
            }

            // Nothing to do
            return Pdf.List();
        }
    }
}
