namespace DdLogMon.Models
{
    public class ConfigSettings
    {
        public int StatsSummaryDisplayInterval { get; set; }
        public int TailInactivityTimeout { get; set; }
        public int MaxRetryTailAttempts { get; set; }
        public int BackoffRetrySeconds { get; set; }
        public string DefaultHttpAccessFile { get; set; }
        public bool ProcessExistingLogLines { get; set; }
        public int TotalTrafficIntervalMinutes { get; set; }
        public int TotalTrafficRequestsPerSecondThreshold { get; set; }
    }
}
