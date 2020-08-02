using System;

namespace Rotvel.PdfConverter
{
    public class PageEventArgs : EventArgs
    {
        public PageEventArgs(string page, int number = 0)
        {
            Name = page;
            Number = number;
        }

        public int Number;

        public string Name { get; private set; }
    }
}
