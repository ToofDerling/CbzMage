namespace CbzMage.Shared.Jobs
{
    public interface IJob<T>
    {
        Task<T> ExecuteAsync();
    }
}
