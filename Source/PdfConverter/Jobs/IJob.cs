namespace PdfConverter.Jobs
{
    public interface IJob<T>
    {
        T Execute();
    }
}
