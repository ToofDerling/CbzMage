using AzwConverter;

namespace CbzMage
{
    internal class Program
    {
        public static string _usage = @"
Azw:
    Azw Convert [or AzwConvert] 
    Scans awz directory and converts all unconverted books to cbz files.
    
    Azw Convert [or AzwConvert] <.NEW or .UPDATED title> or <directory with .NEW and .UPDATED titles>
    Converts .NEW and .UPDATED title(s) to cbz files.    

    Azw ScanNew [or AzwScanNew] 
    Scans azw directory and creates a .NEW title for each unconverted book. 
    
    Azw ScanUpdated [or AzwScanUpdated]
    Scans azw directory and creates a .NEW title for each unconverted book and a .UPDATED title for each
    book that has been updated.

Pdf:
    Nothing yet...    

(All commands are case insensitive.).
";
        static void Main(string[] args)
        {
            //var action = AzwAction.Scan;
            var action = AzwAction.Convert;

            var converter = new AzwConverter.AzwConverter(action, null);
            converter.ConvertOrScan();

            //Console.WriteLine(_usage);
            Console.ReadLine();
        }
    }
}