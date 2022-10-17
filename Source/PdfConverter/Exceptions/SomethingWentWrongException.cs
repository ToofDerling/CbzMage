namespace PdfConverter.Exceptions
{
    public class SomethingWentWrongSorryException : ApplicationException
    {
        public SomethingWentWrongSorryException(string message) : base($"Sorry: {message}")
        {
        }
    }
}
