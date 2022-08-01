using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DdLogMon.Helpers;
using DdLogMon.Interfaces;
using DdLogMon.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DdLogMon.Services
{
    internal class RetryBackoffTailFileFailureDelegate : ITailFileFailureDelegate
    {
        private readonly Dictionary<TailFileInfo, RetryBackoffFileInfo> _fileRetryInfo = new Dictionary<TailFileInfo, RetryBackoffFileInfo>();

        private readonly ILogger<RetryBackoffTailFileFailureDelegate> _logger;
        private readonly IOptions<ConfigSettings> _settings;
        private readonly ITailFileService _tailFileService;

        public RetryBackoffTailFileFailureDelegate(ILogger<RetryBackoffTailFileFailureDelegate> logger,
                                                   IOptions<ConfigSettings> settings,
                                                   ITailFileService tailFileService)
        {
            _logger = logger;
            _settings = settings;
            _tailFileService = tailFileService;
        }

        public void OnFailure(TailFileInfo tailFile, Exception exception)
        {
            // Log, stop tailing as needed, then retry after a wait if appropriate....
            _logger.LogError(exception, $"Tailing file [{tailFile}] failed");

            _tailFileService.StopTail(tailFile);

            _fileRetryInfo.TryAdd(tailFile, new RetryBackoffFileInfo());

            var info = _fileRetryInfo[tailFile];

            if (info.RetryAttempts >= _settings.Value.MaxRetryTailAttempts.Gz(5))
            {
                _logger.LogError($"File [{tailFile}] has been attempted [{info.RetryAttempts}] times and will not be retried again");
                return;
            }

            info.RetryAttempts++;

            // Backoff incrementally based on how many times we've retried...wait, then retry...
            var backoffMs = info.RetryAttempts * _settings.Value.BackoffRetrySeconds.Gz(3) * 1000;

            Task.Delay(backoffMs).Wait();

            // Re-attempt tailing...
            _tailFileService.Tail(tailFile.FileToTail, tailFile.FileType);
        }

        private class RetryBackoffFileInfo
        {
            public int RetryAttempts { get; set; }
        }
    }
}
