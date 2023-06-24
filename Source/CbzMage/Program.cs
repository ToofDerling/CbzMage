using AzwConverter.Converter;
using CbzMage.Shared;
using CbzMage.Shared.Helpers;
using EpubConverter;
using PdfConverter;
using System.Runtime.InteropServices;

namespace CbzMage
{
    internal class Program
    {
        public static string _usage = @"
AzwConvert [or Azw Convert] <file or directory>
    Scans AzwDir and converts all unconverted comic books to cbz files.
    Specify <file or directory> to convert azw/azw3 files directly.  

AzwScan [or Azw Scan] <file or directory>
    Scans AzwDir and creates a .NEW title file for each unconverted comic book. 
    Specify <file or directory> to scan azw/azw3 files directly.  

PdfConvert [or Pdf Convert] <pdf file> or <directory with pdf files>
    Converts one or more pdf comic books to cbz files.

EpubConvert [or Epub Convert] <epub file> or <directory with epub files>
    Converts one or more epub comic books to cbz files.

Commands are case insensitive. 
";
        /*
        BlackSteedConvert [BlackSteed Convert] <directory>
            Convert one or more Black Steed comic books copied from a mobile device.
        */

        static async Task Main(string[] args)
        {
            Console.CursorVisible = false;
            Console.CancelKeyPress += (_, _) => Console.CursorVisible = true;

            var validAction = false;
            CbzMageAction action;

            var actionStr = string.Empty;
            var next = 0;

            args = new[] { "EpubConvert" };

            if (args.Length > next)
            {
                ParseActionString();

                if (args.Length > next && !validAction)
                {
                    ParseActionString();
                }

                try
                {
                    if (validAction)
                    {
                        var path = args.Length > next ? args[next].Trim() : string.Empty;

                        switch (action)
                        {
                            case CbzMageAction.AzwScan:
                            case CbzMageAction.AzwConvert:
                                if (path.Length > 0)
                                {
                                    var fileOrDirConverter = new AzwFileOrDirectoryConverter(action, path);
                                    await fileOrDirConverter.ConvertOrScanAsync();
                                }
                                else
                                {
                                    var azwConverter = new AzwLibraryConverter(action);
                                    await azwConverter.ConvertOrScanAsync();
                                }
                                break;
                            case CbzMageAction.AzwAnalyze:
                                {
                                    var azwConverter = new AzwLibraryConverter(action);
                                    await azwConverter.ConvertOrScanAsync();
                                }
                                break;
                            case CbzMageAction.PdfConvert:
                                {
                                    var pdfConverter = new PdfFileOrDirectoryConverter();
                                    pdfConverter.ConvertFileOrDirectory(path!);
                                }
                                break;
                            case CbzMageAction.BlackSteedConvert:
                                {
                                    var blackSteedConverter = new BlackSteedConverter.BlackSteedConverter();
                                    await blackSteedConverter.ConvertDirectoryAsync(path);
                                }
                                break;
                            case CbzMageAction.EpubConvert:
                                {
                                    var epubConverter = new EpubFileOrDirectoryConverter();
                                    await epubConverter.ConvertFileOrDirectoryAsync(path);
                                }
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ProgressReporter.Error("CbzMage fatal error.", ex);
                }
            }

            if (!validAction)
            {
                ProgressReporter.Info(_usage);
            }

            // If this is run as a "gui" let the console hang around
            if (ConsoleWillBeDestroyedAtTheEnd())
            {
                Console.ReadLine();
            }

            Console.CursorVisible = true;

            void ParseActionString()
            {
                actionStr += args[next];

                validAction = Enum.TryParse(actionStr, ignoreCase: true, out action);

                next++;
            }
        }

        private static bool ConsoleWillBeDestroyedAtTheEnd()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                return false;
            }

            var processList = new uint[1];
            var processCount = GetConsoleProcessList(processList, 1);

            return processCount == 1;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint GetConsoleProcessList(uint[] processList, uint processCount);
    }
}