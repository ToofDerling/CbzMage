using System;

namespace Rotvel.PdfConverter.Helpers
{
    public class SomethingWentWrongException : ApplicationException
    {
        public SomethingWentWrongException(string message) : base(message)
        {
        }
    }
}
