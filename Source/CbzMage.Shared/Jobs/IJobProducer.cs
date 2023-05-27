using System.Collections.Concurrent;

namespace CbzMage.Shared.Jobs
{
    public interface IJobProducer<T>
    {
        Task ProduceAsync(BlockingCollection<IJobConsumer<T>> jobQueue);
    }
}
