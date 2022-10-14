namespace PdfConverter.Events
{
    public class ExtractedEventArgs
    {
        public string Progress { get; private set; }

        public ExtractedEventArgs(string progress)
        {
            Progress = progress;
        }
    }
}
