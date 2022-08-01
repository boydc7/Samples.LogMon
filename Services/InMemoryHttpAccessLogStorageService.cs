using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DdLogMon.Helpers;
using DdLogMon.Interfaces;
using DdLogMon.Models;

namespace DdLogMon.Services
{
    public class InMemoryHttpAccessLogStorageService : IHttpAccessLogStorageService
    {
        private readonly int _minutesOfDataToKeep;
        private readonly int _minutesBackToKeep;

        private readonly ConcurrentQueue<HttpAccessLogLine> _lines = new ConcurrentQueue<HttpAccessLogLine>();
        private readonly Timer _timer;
        private int _isMaintaining;

        public InMemoryHttpAccessLogStorageService(int minutesOfDataToKeep = 3)
        {
            _minutesOfDataToKeep = minutesOfDataToKeep;
            _minutesBackToKeep = minutesOfDataToKeep.Gz(3) * -1;
            _timer = new Timer(Maintain, null, TimeSpan.Zero, TimeSpan.FromSeconds(15));
        }

        private DateTime MinTimeToKeep() => DateTime.UtcNow.AddMinutes(_minutesBackToKeep);
        private DateTime MaxTimeToKeep() => DateTime.UtcNow.AddMinutes(_minutesOfDataToKeep);

        public void StoreLine(HttpAccessLogLine line)
        {
            if (line.ReceivedOn <= MinTimeToKeep() || line.ReceivedOn > MaxTimeToKeep())
            {
                return;
            }

            _lines.Enqueue(line);
        }

        public IEnumerable<HttpAccessLogLine> GetLinesAfter(DateTime onOrAfter)
            => _lines.Where(l => l.ReceivedOn >= onOrAfter)
                     .Where(l => l.ReceivedOn <= DateTime.UtcNow);

        private void Maintain(object state)
        {
            if (_lines.IsEmpty)
            {
                return;
            }

            var isMaintaining = Interlocked.CompareExchange(ref _isMaintaining, 1, 0);

            if (isMaintaining > 0)
            {   // Already being handled....
                return;
            }

            try
            {
                RemoveLinesBefore(MinTimeToKeep());
            }
            finally
            {
                Interlocked.CompareExchange(ref _isMaintaining, 0, 1);
            }
        }

        public void RemoveLinesBefore(DateTime onOrBefore)
        {
            while (_lines.TryPeek(out var line) && line.ReceivedOn <= onOrBefore)
            {   // For now I just assume this is the only thing that pulls off the queue, so this is a safe operation given that assumption...
                _lines.TryDequeue(out _);
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
