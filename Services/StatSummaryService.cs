using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DdLogMon.Helpers;
using DdLogMon.Interfaces;
using DdLogMon.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DdLogMon.Services
{
    public class StatSummaryService : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly int _runIntervalSeconds;
        private readonly List<IStatSummaryLogger> _loggers;

        public StatSummaryService(IOptions<ConfigSettings> settings,
                                  ILogger<StatSummaryService> logger,
                                  IHttpAccessLogStorageService httpAccessLogStorageService)
        {
            _runIntervalSeconds = settings.Value.StatsSummaryDisplayInterval.Gz(10);

            // The loggers that actually do things....
            _loggers = new List<IStatSummaryLogger>
                       {
                           new IntervalSummaryLogger(settings, logger, httpAccessLogStorageService),
                           new TotalTrafficSummaryLogger(settings.Value.TotalTrafficIntervalMinutes,
                                                         settings.Value.TotalTrafficRequestsPerSecondThreshold,
                                                         logger,
                                                         httpAccessLogStorageService)
                       };
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(Process, null, TimeSpan.Zero, TimeSpan.FromSeconds(_runIntervalSeconds));

            return Task.CompletedTask;
        }

        private void Process(object state)
            => _loggers.Each(l => l.Log());

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
