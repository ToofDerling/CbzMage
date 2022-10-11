using AzwConverter;

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
            var validAction = false;

            string actionStr;

            if (args.Length > 0)
            {
                actionStr = args[0];
                if (args.Length > 1)
                {
                    actionStr += args[1];
                }
            
                if (Enum.TryParse(typeof(AzwAction), actionStr, ignoreCase: true, out var action))
                {
                    validAction = true;

                    var converter = new AzwConverter.AzwConverter((AzwAction)action, null);
                    converter.ConvertOrScan();
                }
            }


            if (!validAction)
            {
                Console.WriteLine(_usage);
            }

            Console.ReadLine();
        }
    }
}