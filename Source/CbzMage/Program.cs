using AzwConverter.Converter;
using BlackSteedConverter;
using CbzMage.Shared;
using CbzMage.Shared.Helpers;
using PdfConverter;
using System.Runtime.InteropServices;

namespace CbzMage
{
    internal class Program
    {
        public static string _usage = @"
AzwConvert [or Azw Convert]
    Scans awz directory and converts all unconverted comic books to cbz files.

AzwScan [or Azw Scan]
    Scans azw directory and creates a .NEW title file for each unconverted comic book. 

PdfConvert [or Pdf Convert] <pdf file> or <directory with pdf files>
    Converts one or more pdf comic books to cbz files.

BlackSteedConvert [BlackSteed Convert] <directory>
    Convert one or more Black Steed comic books copied from a mobile device.

Commands are case insensitive. 
";
        static async Task Main(string[] args)
        {
            var validAction = false;
            CbzMageAction action;

            var actionStr = string.Empty;
            var next = 0;

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
                        var path = args.Length > next ? args[next] : null;

                        switch (action)
                        {
                            case CbzMageAction.AzwScan:
                            case CbzMageAction.AzwConvert:
                                if (!string.IsNullOrEmpty(path))
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
                                    blackSteedConverter.ConvertDirectory(path);
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

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                // If this is run as a "gui" let the console hang around
                if (ConsoleWillBeDestroyedAtTheEnd())
                {
                    Console.ReadLine();
                }
            }

            void ParseActionString()
            {
                actionStr += args[next];

                validAction = Enum.TryParse(actionStr, ignoreCase: true, out action);

                next++;
            }
        }

        private static bool ConsoleWillBeDestroyedAtTheEnd()
        {
            var processList = new uint[1];
            var processCount = GetConsoleProcessList(processList, 1);

            return processCount == 1;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint GetConsoleProcessList(uint[] processList, uint processCount);
    }
}