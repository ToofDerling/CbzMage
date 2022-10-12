using AzwConverter;
using System.Runtime.InteropServices;

namespace CbzMage
{
    internal class Program
    {
        public static string _usage = @"
All commands are case insensitive.

AzwConvert [or Azw Convert]
    Scans awz directory and converts all unconverted books to cbz files.

AzwScan [or Azw Scan]
    Scans azw directory and creates a .NEW title file for each unconverted book. 

In both cases CbzMage will scan for updated books and create an .UPDATED title 
file for each updated book. 

Pdf:
    Nothing yet...    
";
        static void Main(string[] args)
        {
#if DEBUG
            args = new[] {"AzwScan"};
#endif
            var validAction = false;

            string actionStr;

            if (args.Length > 0)
            {
                actionStr = args[0];
                if (args.Length > 1)
                {
                    actionStr += args[1];
                }

                try
                {
                    if (Enum.TryParse(typeof(AzwAction), actionStr, ignoreCase: true, out var action))
                    {
                        validAction = true;

                        var converter = new AzwConverter.AzwConverter((AzwAction)action, null);
                        converter.ConvertOrScan();
                    }
                }
                catch (Exception ex)
                { 
                    Console.WriteLine(ex.ToString()); 
                }
            }

            if (!validAction)
            {
                Console.WriteLine(_usage);
            }

            // If this is run as a "gui" let the console hang around
            if (ConsoleWillBeDestroyedAtTheEnd())
            {
                Console.ReadLine();
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