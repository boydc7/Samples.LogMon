using System;

namespace DdLogMon.Interfaces
{
    public interface IFileSystemTailDelegate : IDisposable
    {
        void StartTail();
        void StopTail();
    }
}
