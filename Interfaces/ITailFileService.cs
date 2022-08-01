using System.Threading.Tasks;
using DdLogMon.Models;

namespace DdLogMon.Interfaces
{
    public interface ITailFileService
    {
        void Tail(string fileToTail, TailFileType fileType);
        void StopTail(string tailFile);
        void StopTail(TailFileInfo fileInfo);
    }
}
