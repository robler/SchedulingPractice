using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using SchedulingPractice.Core;

namespace SubWorker.BobDemo
{
    public class BobSubWorkerBackgroundService : BackgroundService
    {
        private readonly QueueManager _pool;
        private readonly int _threadCount = 5;
        private CancellationToken _cancellationToken;
        public BobSubWorkerBackgroundService()
        {
            _pool = new QueueManager(_threadCount, _cancellationToken);
            Console.WriteLine("Bob Start..");
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _cancellationToken= stoppingToken;

            await Task.Delay(1, _cancellationToken);

            using (JobsRepo repo = new JobsRepo())
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var jobs = repo.GetReadyJobs(JobSettings.MinPrepareTime);
                        var counter = 0;
                        foreach (var job in jobs)
                        {
                            if (_cancellationToken.IsCancellationRequested) goto shutdown;
                            _pool.AddJob(job);
                            counter++;
                        }
                        Console.WriteLine($"got {counter} jobs...");

                        var waitTime = JobSettings.MinPrepareTime;

                        if (jobs.Any())
                            waitTime = jobs.Last().RunAt - DateTime.Now;

                        await Task.Delay(waitTime, _cancellationToken);
                        Console.WriteLine($" insert jobs, wait {(int)waitTime.TotalMilliseconds} msec");
                    }
                    catch (TaskCanceledException ex)
                    {
                        Console.WriteLine($" exception raised : {ex} ");
                        goto shutdown;
                    }

                }
                Console.WriteLine($"Cancellation Requested.");
            }

        shutdown:
            Console.WriteLine($"shutdown event detected, stop worker service...");
        }
    }
}