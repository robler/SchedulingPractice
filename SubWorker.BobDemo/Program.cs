using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace SubWorker.BobDemo
{
    class Program
    {
        private static void Main()
        {
            var host = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddHostedService<BobSubWorkerBackgroundService>();
                })
                .Build();

            using (host)
            {
                host.Start();
                host.WaitForShutdown();
            }
        }
    }
}