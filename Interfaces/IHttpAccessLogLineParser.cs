using DdLogMon.Models;

namespace DdLogMon.Interfaces
{
    public interface IHttpAccessLogLineParser
    {
        HttpAccessLogLine Parse(string line);
    }
}
