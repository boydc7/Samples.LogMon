using DdLogMon.Interfaces;
using Microsoft.Extensions.Logging;

namespace DdLogMon.Services
{
    public class HttpAccessLogFileLineProcessor : IFileLineProcessor
    {
        private readonly IHttpAccessLogStorageService _storageService;
        private readonly ILogger<HttpAccessLogFileLineProcessor> _logger;
        private readonly IHttpAccessLogLineParser _lineParser;

        public HttpAccessLogFileLineProcessor(IHttpAccessLogStorageService storageService,
                                              ILogger<HttpAccessLogFileLineProcessor> logger,
                                              IHttpAccessLogLineParser lineParser)
        {
            _storageService = storageService;
            _logger = logger;
            _lineParser = lineParser;
        }

        public void ProcessLine(string line)
        {
            var lineModel = _lineParser.Parse(line);

            if (lineModel == null)
            {
                return;
            }

            _storageService.StoreLine(lineModel);
        }
    }
}
