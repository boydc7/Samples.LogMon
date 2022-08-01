using DdLogMon.Interfaces;

namespace DdLogMon.Services
{
    public class NullFileLineProcessor : IFileLineProcessor
    {
        private NullFileLineProcessor() { }

        public static NullFileLineProcessor Instance { get; } = new NullFileLineProcessor();

        public void ProcessLine(string line) { }
    }
}
