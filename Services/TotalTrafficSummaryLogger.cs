using System;
using System.Linq;
using DdLogMon.Helpers;
using DdLogMon.Interfaces;
using Microsoft.Extensions.Logging;

namespace DdLogMon.Services
{
    public class TotalTrafficSummaryLogger : IStatSummaryLogger
    {
        private readonly object _lockObject = new object();
        private bool _isProcessing;
        private bool _inAlarm;

        private readonly int _totalTrafficIntervalInMinutes;
        private readonly int _totalTrafficRequestsPerSecondThreshold;
        private readonly ILogger<StatSummaryService> _logger;
        private readonly IHttpAccessLogStorageService _httpAccessLogStorageService;
        private readonly Action<double, bool> _onAlarmStateChangedd;

        public TotalTrafficSummaryLogger(int totalTrafficIntervalInMinutes,
                                         int totalTrafficRequestsPerSecondThreshold,
                                         ILogger<StatSummaryService> logger,
                                         IHttpAccessLogStorageService httpAccessLogStorageService,
                                         Action<double, bool> onAlarmStateChangedd = null)
        {
            _totalTrafficIntervalInMinutes = totalTrafficIntervalInMinutes.Gz(2);
            _totalTrafficRequestsPerSecondThreshold = totalTrafficRequestsPerSecondThreshold.Gz(10);
            _logger = logger;
            _httpAccessLogStorageService = httpAccessLogStorageService;
            _onAlarmStateChangedd = onAlarmStateChangedd;
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
                var totalTrafficInterval = _totalTrafficIntervalInMinutes * 60.0;

                var totalRequests = _httpAccessLogStorageService.GetLinesAfter(DateTime.UtcNow.AddSeconds(totalTrafficInterval * -1))
                                                                .Count();

                var requestsPerSecond = totalRequests / totalTrafficInterval;

                if (requestsPerSecond < _totalTrafficRequestsPerSecondThreshold)
                {
                    if (_inAlarm)
                    {
                        _inAlarm = false;

                        _onAlarmStateChangedd?.Invoke(requestsPerSecond, _inAlarm);

                        _logger.LogInformation($"No longer in high traffic alarm state - hits are {requestsPerSecond} at {DateTime.Now}");
                    }

                    return;
                }

                _inAlarm = true;

                _onAlarmStateChangedd?.Invoke(requestsPerSecond, _inAlarm);

                var logString = $"High traffic generated an alert - hits = {requestsPerSecond}, triggered at {DateTime.Now}";

                _logger.LogWarning(logString);
            }
            finally
            {
                _isProcessing = false;
            }
        }
    }
}
