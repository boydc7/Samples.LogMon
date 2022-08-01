using System;
using System.Net;

namespace DdLogMon.Models
{
    public class HttpAccessLogLine
    {
        public string IpAddress { get; set; }
        public string Ident { get; set; }
        public string UserId { get; set; }
        public DateTime ReceivedOn { get; set; }
        public string Request { get; set; }
        public int StatusCode { get; set; }
        public long ResponseSize { get; set; }
    }
}
