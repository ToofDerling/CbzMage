using System;

namespace CoreComicsConverter.PdfFlow
{
    public class PdfEncryptedException : Exception
    {
        public PdfEncryptedException(string message) : base(message)
        {
        }
    }
}
