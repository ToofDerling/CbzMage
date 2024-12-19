namespace PdfConverter.ImageProducer
{
    public abstract class AbstractImageProducer
    {
        public Pdf Pdf { get; private set; }

        public List<int> PageList { get; private set; }

        protected bool IsStarted { get; set; }

        public AbstractImageProducer(Pdf pdf, List<int> pageList)
        {
            Pdf = pdf;
            PageList = pageList;
        }

        protected void EnsureStarted()
        {
            if (!IsStarted)
            {
                throw new InvalidOperationException("Start() not called!");
            }
        }

        public abstract void Start(IImageDataHandler imageDataHandler);

        public abstract ICollection<string> GetErrors();

        public abstract int WaitForExit();

        public abstract void Dispose();
    }
}
