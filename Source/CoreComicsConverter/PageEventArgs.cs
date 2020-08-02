using System;

namespace CoreComicsConverter
{
    public class PageEventArgs : EventArgs
    {
        public PageEventArgs(string page, int number = 0)
        {
            Name = page;
            Number = number;
        }

        public int Number { get; private set; }

        public string Name { get; private set; }
    }
}
