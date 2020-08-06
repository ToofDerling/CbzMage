using System;

namespace CoreComicsConverter.Model
{
    public class PdfEncryptedException : Exception
    {
        public PdfEncryptedException(string message) : base(message)
        { 
        }
    }
}
