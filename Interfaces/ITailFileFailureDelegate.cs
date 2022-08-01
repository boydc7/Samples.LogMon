using System;
using DdLogMon.Models;

namespace DdLogMon.Interfaces
{
    public interface ITailFileFailureDelegate
    {
        void OnFailure(TailFileInfo tailFile, Exception exception);
    }
}
