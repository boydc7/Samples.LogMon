using System;
using System.Collections.Generic;
using DdLogMon.Models;

namespace DdLogMon.Interfaces
{
    public interface IHttpAccessLogStorageService : IDisposable
    {
        void StoreLine(HttpAccessLogLine line);
        IEnumerable<HttpAccessLogLine> GetLinesAfter(DateTime onOrAfter);
    }
}
