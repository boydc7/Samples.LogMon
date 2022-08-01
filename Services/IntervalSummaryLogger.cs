using System;
using System.Linq;
using DdLogMon.Helpers;
using DdLogMon.Interfaces;
using DdLogMon.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DdLogMon.Services
{
    internal class IntervalSummaryLogger : IStatSummaryLogger
    {
        private readonly object _lockObject = new object();
        private bool _isProcessing;

        private readonly IOptions<ConfigSettings> _settings;
        private readonly ILogger<StatSummaryService> _logger;
        private readonly IHttpAccessLogStorageService _httpAccessLogStorageService;
        private readonly int _runIntervalSeconds;

        public IntervalSummaryLogger(IOptions<ConfigSettings> settings,
                                     ILogger<StatSummaryService> logger,
                                     IHttpAccessLogStorageService httpAccessLogStorageService)
        {
            _settings = settings;
            _logger = logger;
            _httpAccessLogStorageService = httpAccessLogStorageService;
            _runIntervalSeconds = _settings.Value.StatsSummaryDisplayInterval.Gz(10);
        }

        public void Log()
        {
            if (_isProcessing)
            {
                return;
            }

            lock(_lockObject)
            {
                if (_isProcessing)
                {
                    return;
                }

                _isProcessing = true;
            }

            try
            {
                var aggBySection = _httpAccessLogStorageService.GetLinesAfter(DateTime.UtcNow.AddSeconds(_runIntervalSeconds * -1))
                                                               .GroupBy(l => l.Request.GetStatsSectionFromRequest())
                                                               .Select(g => new
                                                                            {
                                                                                Section = g.Key,
                                                                                SuccessCount = g.Count(l => l.StatusCode >= 200 && l.StatusCode < 300),
                                                                                TotalCount = g.Count(),
                                                                                TotalResponseSize = g.Sum(l => l.ResponseSize)
                                                                            })
                                                               .OrderByDescending(t => t.TotalCount)
                                                               .ToList();

                var totalRequests = aggBySection.Sum(t => t.TotalCount);

                var logString = $@"Stats last [{_runIntervalSeconds}] seconds:
   TotalRequests:   [{totalRequests}]
   SuccessRequests: [{aggBySection.Sum(t => t.SuccessCount)}]
   AvgPerSecond:    [{(totalRequests * 1.0) / _runIntervalSeconds}]
   Top 3 Sections:
{string.Join(Environment.NewLine, aggBySection.Take(3).Select(s => $"      {s.Section} - {s.TotalCount}"))}
";

                _logger.LogInformation(logString);
            }
            finally
            {
                _isProcessing = false;
            }
        }
    }
}
