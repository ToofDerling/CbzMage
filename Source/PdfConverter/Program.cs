using CbzMage.Shared.Extensions;
using Ghostscript.NET;
using PdfConverter.Ghostscript;
using PdfConverter.Helpers;
using PdfConverter.ManagedBuffers;
using System.Diagnostics;
using System.Reflection;

namespace PdfConverter
{
    public class Program
    {

#if DEBUG
        private static readonly string _testPdf = @"M:\Data\Pdf\Test";
#else
        private static readonly string _testPdf = null;
#endif

        // Holds png files read from pdf. Will be expanded if needed (largest png reported so far: 15 MB)
        private const int BufferSize = 20000000;

        public static void Main(string[] args)
        {
            var pdfList = InitializePdfPath(args);

            if (pdfList.Any())
            {
                var bin = Assembly.GetExecutingAssembly().Location;
                var gsPath = Path.Combine(Path.GetDirectoryName(bin), "gsdll64.dll");

                var version = new GhostscriptVersionInfo(gsPath);

                // Throws if wrong 32/64 version of Ghostscript installed
                using (var pageMachineManager = new GhostscriptPageMachineManager(version))
                {
                    using var bufferCache = new BufferCache(BufferSize);

                    var converter = new PdfComicConverter(pageMachineManager);
                    pdfList.ForEach(pdf => ConvertPdf(pdf, converter));
                }

                StatsCount.ShowStats();
            }
            else
            {
                Console.WriteLine("PdfConverter <directory|pdf_file>");
            }

            Console.ReadLine();
        }

        private static void ConvertPdf(Pdf pdf, PdfComicConverter converter)
        {
            var stopWatch = Stopwatch.StartNew();

            using var pdfParser = new PdfImageParser(pdf);

            Console.WriteLine(pdf.Path);
            Console.WriteLine($"{pdf.PageCount} pages");

            converter.ConvertToCbz(pdf, pdfParser);

            stopWatch.Stop();
            var passed = stopWatch.Elapsed;

            Console.WriteLine($"{passed.Minutes} min {passed.Seconds} sec");
            Console.WriteLine();
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
                    var files = Directory.GetFiles(path, "*.pdf"); //Search pattern is case insensitive
                    if (files.Length > 0)
                    {
                        return Pdf.List(files.ToArray());
                    }
                }
                else if (File.Exists(path) && path.EndsWithIgnoreCase(".pdf"))
                {
                    return Pdf.List(path);
                }
            }

            //Nothing to do
            return Pdf.List();
        }
    }
}
