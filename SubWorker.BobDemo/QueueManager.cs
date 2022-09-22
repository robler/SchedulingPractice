using SchedulingPractice.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SubWorker.BobDemo
{
    public class QueueManager
    {
        private readonly int _maxThreadCount = 0;
        private readonly Thread[] _workers;
        private readonly BlockingCollection<JobInfo> _queue = new BlockingCollection<JobInfo>();
        private CancellationToken _cancellationToken;

        public QueueManager(int maxThreadsCount, CancellationToken cancellationToken = default)
        {
            _maxThreadCount = maxThreadsCount;
            _workers = new Thread[maxThreadsCount];
            CreateJob();
            _cancellationToken = cancellationToken;
        }

        private void CreateJob()
        {
            for (var i = 0; i < _maxThreadCount; i++)
            {
                var worker = new Thread(ProcessJob);
                _workers[i] = worker;
                worker.Start();
            }

        }
        private void ProcessJob()
        {
            Console.WriteLine($"worker started event.");
            using (JobsRepo repo = new JobsRepo())
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var job = _queue.Take();
                        if (repo.GetJob(job.Id).State != 0 || !repo.AcquireJobLock(job.Id))
                            continue;

                        if (DateTime.Now < job.RunAt)
                            Thread.Sleep(job.RunAt - DateTime.Now);

                        repo.ProcessLockedJob(job.Id);
                        Console.WriteLine($"[T: {Thread.CurrentThread.ManagedThreadId}] process job({job.Id}) with delay {(int)(DateTime.Now - job.RunAt).TotalMilliseconds} msec...");
                    }
                    catch (TaskCanceledException)
                    { 
                    }
                }
            }
        }

        public bool AddJob(JobInfo job)
        {
            _queue.Add(job);
            return true;
        }
    }


}
