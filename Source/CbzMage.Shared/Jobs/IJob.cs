namespace CbzMage.Shared.Jobs
{
    public interface IJob<T>
    {
        T Execute();
    }
}
