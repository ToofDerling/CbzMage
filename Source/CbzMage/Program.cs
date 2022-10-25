using CbzMage.Shared;
using CbzMage.Shared.Helpers;
using System.Runtime.InteropServices;

namespace CbzMage
{
    internal class Program
    {
        public static string _usage = @"
Commands are case insensitive. 

AzwConvert [or Azw Convert]
    Scans awz directory and converts all unconverted books to cbz files.

AzwScan [or Azw Scan]
    Scans azw directory and creates a .NEW title file for each unconverted book. 

In both cases CbzMage will scan for updated books and create an .UPDATED title 
file for each updated book. 

PdfConvert [or Pdf Convert] <pdf file> or <directory with pdf files>
    Converts one or more pdf comic books to cbz files (DOES NOT WORK YET).
";
        static void Main(string[] args)
        {
#if DEBUG
            args = new[] { CbzMageAction.PdfConvert.ToString() };
#endif
            var validAction = false;

            string actionStr;
            var next = 0;

            if (args.Length > next)
            {
                actionStr = args[next];
                next++;

                if (args.Length > next)
                {
                    actionStr += args[next];
                    next++;
                }

                try
                {
                    if (Enum.TryParse(typeof(CbzMageAction), actionStr, ignoreCase: true, out var actionObj))
                    {
                        var action = (CbzMageAction)actionObj;
                        validAction = true;

                        var path = args.Length > next ? args[next] : null;

                        switch (action)
                        {
                            case CbzMageAction.AzwScan:
                            case CbzMageAction.AzwConvert:
                                var azwConverter = new AzwConverter.AzwConverter(action);
                                azwConverter.ConvertOrScan();
                                break;
                            case CbzMageAction.PdfConvert:
                                var pdfConverter = new PdfConverter.PdfConverter();
                                pdfConverter.ConvertFileOrDirectory(path);
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