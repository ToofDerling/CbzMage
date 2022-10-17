namespace PdfConverter.Exceptions
{
    public class SomethingWentWrongException : ApplicationException
    {
        public SomethingWentWrongException(string message) : base(message)
        {
        }
    }
}
