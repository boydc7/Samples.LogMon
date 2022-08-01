using System;
using System.Linq;
using DdLogMon.Helpers;
using DdLogMon.Models;
using DdLogMon.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace DdLogMon.UnitTests
{
    [TestFixture]
    public class SummaryLoggerTests
    {
        [Test]
        public void TotalTrafficLoggerEntersAndExistsAlarmStateCorrectly()
        {
            var storageService = new InMemoryHttpAccessLogStorageService();

            bool? inAlarmState = null;
            double? requestsPerSecond = null;

            var logger = new TotalTrafficSummaryLogger(2, 10, NullLogger<StatSummaryService>.Instance, storageService,
                                                       (r, s) =>
                                                       {
                                                           requestsPerSecond = r;
                                                           inAlarmState = s;
                                                       });

            // Load up to 8/s for 2 minutes...
            Enumerable.Range(1, 1199).Select(i => new HttpAccessLogLine
                                                  {
                                                      ReceivedOn = DateTime.UtcNow.AddSeconds((i % 120) * -1),
                                                      Request = "GET /dummy/dummy2/dummy3 HTTP/1.1",
                                                      StatusCode = 200,
                                                      ResponseSize = 1000
                                                  })
                      .OrderBy(l => l.ReceivedOn)
                      .Each(l => storageService.StoreLine(l));

            logger.Log();

            // Should not even have fired (i.e. should still be in the original, non-alarm state, which would not fire an on-alarm-change event)
            requestsPerSecond.Should().BeNull();
            inAlarmState.Should().BeNull();

            // Load up enough to push it to 10
            Enumerable.Range(1, 10).Select(i => new HttpAccessLogLine
                                                {
                                                    ReceivedOn = DateTime.UtcNow.AddSeconds((i % 120) * -1),
                                                    Request = "GET /dummy/dummy2/dummy3 HTTP/1.1",
                                                    StatusCode = 200,
                                                    ResponseSize = 1000
                                                })
                      .OrderBy(l => l.ReceivedOn)
                      .Each(l => storageService.StoreLine(l));

            logger.Log();

            // Now should have changed
            requestsPerSecond.Should().BeGreaterThan(10);
            inAlarmState.Should().BeTrue();

            // Remove roughly half of everything
            storageService.RemoveLinesBefore(DateTime.UtcNow.AddSeconds(-60));

            logger.Log();

            // Should have changed back out of alarm state
            requestsPerSecond.Should().BeLessThan(10);
            inAlarmState.Should().BeFalse();
        }
    }
}
