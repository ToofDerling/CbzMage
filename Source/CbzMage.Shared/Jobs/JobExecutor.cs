using System.Collections.Concurrent;

namespace CbzMage.Shared.Jobs
{
    public class JobExecutor<T>
    {
        private class InternalJobWaiter : JobWaiter
        {
            public void SignalWaitIsOver()
            {
                _waitingQueue.Add("Stop");
            }
        }

        private BlockingCollection<IJob<T>> _runningQueue;

        private InternalJobWaiter _jobWaiter;

        private readonly ThreadPriority _threadPriority;

        private readonly int _numThreads;

        private int _numFinishedThreads = 0;

        public JobExecutor(ThreadPriority threadPriority = ThreadPriority.Normal, int numThreads = 1)
        {
            _numThreads = numThreads;
            _threadPriority = threadPriority;
        }

        public JobWaiter Start(bool withWaiter)
        {
            _runningQueue = new BlockingCollection<IJob<T>>();

            if (withWaiter)
            {
                _jobWaiter = new InternalJobWaiter();
            }

            for (int i = 0; i < _numThreads; i++)
            {
                var jobThread = new Thread(new ThreadStart(JobExecutorLoop))
                {
                    Priority = _threadPriority
                };

                jobThread.Start();
            }

            return _jobWaiter;
        }

        public void Stop()
        {
            _runningQueue.CompleteAdding();
        }

        public void AddJob(IJob<T> job)
        {
            _runningQueue.Add(job);
        }

        private void JobExecutorLoop()
        {
            var jobCount = 0;

            foreach (var job in _runningQueue.GetConsumingEnumerable())
            {
                var result = job.Execute();

                jobCount++;

                JobExecuted?.Invoke(this, new JobEventArgs<T>(result));
            }

            if (Interlocked.Increment(ref _numFinishedThreads) == _numThreads)
            {
                _jobWaiter?.SignalWaitIsOver();

                _runningQueue.Dispose();
            }
        }

        public event EventHandler<JobEventArgs<T>> JobExecuted;
    }
}
