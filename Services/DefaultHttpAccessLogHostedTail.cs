using System;
using System.Threading;
using System.Threading.Tasks;
using DdLogMon.Helpers;
using DdLogMon.Interfaces;
using DdLogMon.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DdLogMon.Services
{
    internal class DefaultHttpAccessLogHostedTail : IHostedService, IDisposable
    {
        private readonly ITailFileService _tailFileService;
        private readonly string _httpAccessLogToTail;

        public DefaultHttpAccessLogHostedTail(ITailFileService tailFileService,
                                              IOptions<ConfigSettings> settings,
                                              string httpAccessLogToTail = null)
        {
            _tailFileService = tailFileService;
            _httpAccessLogToTail = httpAccessLogToTail ?? settings.Value.DefaultHttpAccessFile;

            if (!_httpAccessLogToTail.HasValue())
            {
                throw new ArgumentNullException(nameof(httpAccessLogToTail));
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _tailFileService.Tail(_httpAccessLogToTail, TailFileType.HttpAccessLog);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopTailing();

            return Task.CompletedTask;
        }

        private void StopTailing()
            => _tailFileService.StopTail(_httpAccessLogToTail);

        public void Dispose()
            => StopTailing();
    }
}
